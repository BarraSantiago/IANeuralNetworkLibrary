namespace NeuralNetworkLib.Agents
{
    public class FSM<EnumState, EnumFlag>
        where EnumState : Enum
        where EnumFlag : Enum
    {
        private const int UNNASIGNED_TRANSITION = -1;
        private readonly Dictionary<int, Func<object[]>> _behaviourOnEnterParameters;
        private readonly Dictionary<int, Func<object[]>> _behaviourOnExitParameters;
        private readonly Dictionary<int, State> _behaviours;
        private readonly Dictionary<int, Func<object[]>> _behaviourTickParameters;
        private readonly (int destinationInState, Action onTransition)[,] _transitions;
        private int _currentState;

        private readonly ParallelOptions parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 32
        };

        public FSM()
        {
            int states = Enum.GetValues(typeof(EnumState)).Length;
            int flags = Enum.GetValues(typeof(EnumFlag)).Length;
            _behaviours = new Dictionary<int, State>();
            _transitions = new (int, Action)[states, flags];

            for (int i = 0; i < states; i++)
            for (int j = 0; j < flags; j++)
                _transitions[i, j] = (UNNASIGNED_TRANSITION, null);

            _behaviourTickParameters = new Dictionary<int, Func<object[]>>();
            _behaviourOnEnterParameters = new Dictionary<int, Func<object[]>>();
            _behaviourOnExitParameters = new Dictionary<int, Func<object[]>>();
        }

        private BehaviourActions GetCurrentStateOnEnterBehaviours => _behaviours[_currentState]
            .GetOnEnterBehaviour(_behaviourOnEnterParameters[_currentState]?.Invoke());

        private BehaviourActions GetCurrentStateOnExitBehaviours => _behaviours[_currentState]
            .GetOnExitBehaviour(_behaviourOnExitParameters[_currentState]?.Invoke());

        private BehaviourActions GetCurrentStateTickBehaviours => _behaviours[_currentState]
            .GetTickBehaviour(_behaviourTickParameters[_currentState]?.Invoke());

        public void ForceTransition(Enum state)
        {
            ExecuteBehaviour(GetCurrentStateOnExitBehaviours);
            _currentState = Convert.ToInt32(state);
            ExecuteBehaviour(GetCurrentStateOnEnterBehaviours);
        }

        public void AddBehaviour<T>(EnumState stateIndexEnum, Func<object[]> onTickParameters = null,
            Func<object[]> onEnterParameters = null, Func<object[]> onExitParameters = null) where T : State, new()
        {
            int stateIndex = Convert.ToInt32(stateIndexEnum);

            if (_behaviours.ContainsKey(stateIndex)) return;

            State newBehaviour = new T();
            newBehaviour.OnFlag += Transition;
            _behaviours.Add(stateIndex, newBehaviour);
            _behaviourTickParameters.Add(stateIndex, onTickParameters);
            _behaviourOnEnterParameters.Add(stateIndex, onEnterParameters);
            _behaviourOnExitParameters.Add(stateIndex, onExitParameters);
        }

        public void SetTransition(Enum originState, Enum flag, Enum destinationState, Action onTransition = null)
        {
            _transitions[Convert.ToInt32(originState), Convert.ToInt32(flag)] =
                (Convert.ToInt32(destinationState), onTransition);
        }

        private void Transition(Enum flag)
        {
            if (_transitions[_currentState, Convert.ToInt32(flag)].destinationInState == UNNASIGNED_TRANSITION) return;

            ExecuteBehaviour(GetCurrentStateOnExitBehaviours);

            _transitions[_currentState, Convert.ToInt32(flag)].onTransition?.Invoke();

            int currentState = _transitions[_currentState, Convert.ToInt32(flag)].destinationInState;
            if (currentState == UNNASIGNED_TRANSITION) return;
            _currentState = _transitions[_currentState, Convert.ToInt32(flag)].destinationInState;

            ExecuteBehaviour(GetCurrentStateOnEnterBehaviours);
        }


        public void Tick()
        {
            if (!_behaviours.ContainsKey(_currentState)) return;

            ExecuteBehaviour(GetCurrentStateTickBehaviours);
        }


        private void ExecuteBehaviour(BehaviourActions behaviourActions, bool multi = false)
        {
            if (behaviourActions.Equals(default(BehaviourActions))) return;

            int executionOrder = 0;

            while ((behaviourActions.MainThreadBehaviour != null && behaviourActions.MainThreadBehaviour.Count > 0) ||
                   (behaviourActions.MultiThreadablesBehaviour != null &&
                    behaviourActions.MultiThreadablesBehaviour.Count > 0))
            {
                if (multi)
                {
                    ExecuteMultiThreadBehaviours(behaviourActions, executionOrder);
                }
                else
                {
                    ExecuteMainThreadBehaviours(behaviourActions, executionOrder);
                }

                executionOrder++;
            }

            behaviourActions.TransitionBehaviour?.Invoke();
        }

        public void ExecuteBehaviour(BehaviourActions behaviourActions, int executionOrder, bool multi = false)
        {
            if (multi)
            {
                ExecuteMultiThreadBehaviours(behaviourActions, executionOrder);
            }
            else
            {
                ExecuteMainThreadBehaviours(behaviourActions, executionOrder);
            }


            behaviourActions.TransitionBehaviour?.Invoke();
        }

        public int GetMainThreadCount()
        {
            BehaviourActions currentStateBehaviours = GetCurrentStateTickBehaviours;
            if (currentStateBehaviours.MainThreadBehaviour == null)
            {
                return 0;
            }

            return currentStateBehaviours.MainThreadBehaviour.Count;
        }

        public int GetMultiThreadCount()
        {
            BehaviourActions currentStateBehaviours = GetCurrentStateTickBehaviours;
            return currentStateBehaviours.MultiThreadablesBehaviour == null ? 0 : currentStateBehaviours.MultiThreadablesBehaviour.Count;
        }

        public void ExecuteMainThreadBehaviours(BehaviourActions behaviourActions, int executionOrder)
        {
            if (behaviourActions.MainThreadBehaviour != null)
            {
                if (behaviourActions.MainThreadBehaviour.ContainsKey(executionOrder))
                {
                    foreach (Action action in behaviourActions.MainThreadBehaviour[executionOrder])
                    {
                        action.Invoke();
                    }

                    behaviourActions.MainThreadBehaviour.Remove(executionOrder);
                }
            }
        }

        public void ExecuteMultiThreadBehaviours(BehaviourActions behaviourActions, int executionOrder)
        {
            if (behaviourActions.MultiThreadablesBehaviour == null) return;
            if (!behaviourActions.MultiThreadablesBehaviour.ContainsKey(executionOrder)) return;

            Parallel.ForEach(behaviourActions.MultiThreadablesBehaviour, parallelOptions, behaviour =>
            {
                foreach (Action action in behaviour.Value)
                {
                    action.Invoke();
                }
            });

            //Parallel.ForEach(behaviourActions.MultiThreadablesBehaviour[executionOrder], parallelOptions,
            //    behaviour => { behaviour?.Invoke(); });
            behaviourActions.MultiThreadablesBehaviour.TryRemove(executionOrder, out _);
        }

        public void MultiThreadTick(int executionOrder)
        {
            if (!_behaviours.ContainsKey(_currentState)) return;

            ExecuteBehaviour(GetCurrentStateTickBehaviours, executionOrder, true);
        }

        public void MainThreadTick(int executionOrder)
        {
            if (!_behaviours.ContainsKey(_currentState)) return;

            ExecuteBehaviour(GetCurrentStateTickBehaviours, executionOrder, false);
        }
    }
}