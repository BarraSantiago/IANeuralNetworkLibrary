using NeuralNetworkDirectory;
using NeuralNetworkLib;

class Program
{
    private static bool warningState = false;
    private static bool errorState = false;
    private static bool epochState = false;
    private static bool stateTransitionState = false;
    private static bool actionDoneState = false;
    private static bool simulationState = false;
    
    public static EcsPopulationManager populationManager = new EcsPopulationManager();
    static void Main(string[] args)
    {
        populationManager.Awake();
        epochState = true;
        ConsoleLogger.SetLogTypeEnabled(LogType.Epoch, epochState);
        ConsoleLogger.SetLogTypeEnabled(LogType.Warning, false);
        ConsoleLogger.SetLogTypeEnabled(LogType.Error, false);
        ConsoleLogger.SetLogTypeEnabled(LogType.StateTransition, false);
        ConsoleLogger.SetLogTypeEnabled(LogType.ActionDone, false);
        ConsoleLogger.SetLogTypeEnabled(LogType.Simulation, false);
        
        RunSimulation();
    }

    static void RunSimulation()
    {
        const int targetFps = 30;
        const float targetFrameTime = 1000f / targetFps;

        bool isRunning = true;
        DateTime lastUpdateTime = DateTime.Now;

        Console.WriteLine("Simulation started");
        Console.WriteLine("Press 'Q' to quit, 'P' to pause/resume.");
        Console.WriteLine("Press 'F1' for warning messages, 'F2' for error messages.");
        Console.WriteLine("Press 'F3' for epoch messages, 'F4' for state transition messages.");
        Console.WriteLine("Press 'F5' for action done messages, 'F6' for simulation messages.");

        while (isRunning)
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan elapsed = currentTime - lastUpdateTime;
            float deltaTime = (float)elapsed.TotalSeconds;
            lastUpdateTime = currentTime;

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Q:
                        isRunning = false;
                        populationManager.StopSimulation();
                        break;
                    case ConsoleKey.P:
                        populationManager.PauseSimulation();
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Simulation " + (populationManager.isRunning ? "resumed" : "paused"));
                        break;
                    case ConsoleKey.F1:
                        warningState = !warningState;
                        ConsoleLogger.SetLogTypeEnabled(LogType.Warning, warningState);
                        break;
                    case ConsoleKey.F2:
                        errorState = !errorState;
                        ConsoleLogger.SetLogTypeEnabled(LogType.Error, errorState);
                        break;
                    case ConsoleKey.F3:
                        epochState = !epochState;
                        ConsoleLogger.SetLogTypeEnabled(LogType.Epoch, epochState);
                        break;
                    case ConsoleKey.F4:
                        stateTransitionState = !stateTransitionState;
                        ConsoleLogger.SetLogTypeEnabled(LogType.StateTransition, stateTransitionState);
                        break;
                    case ConsoleKey.F5:
                        actionDoneState = !actionDoneState;
                        ConsoleLogger.SetLogTypeEnabled(LogType.ActionDone, actionDoneState);
                        break;
                    case ConsoleKey.F6:
                        simulationState = !simulationState;
                        ConsoleLogger.SetLogTypeEnabled(LogType.Simulation, simulationState);
                        break;
                }
            }

            if (populationManager.isRunning)
            {
                populationManager.Update(deltaTime);
            }

            float frameTime = (float)(DateTime.Now - currentTime).TotalMilliseconds;
            if (frameTime < targetFrameTime)
            {
                int sleepTime = (int)(targetFrameTime - frameTime);
                if (sleepTime > 0)
                {
                    Thread.Sleep(sleepTime);
                }
            }
        }

        Console.WriteLine("Simulation ended.");
    }
}

