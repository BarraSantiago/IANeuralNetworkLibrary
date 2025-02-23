using System.Collections.Concurrent;
using NeuralNetworkLib.ECS.FlockingECS;
using NeuralNetworkLib.ECS.NeuralNetECS;

namespace NeuralNetworkLib.ECS.Patron;

public static class ECSManager
{
    private static ConcurrentDictionary<Type, ECSSystem> systems;
    private static ConcurrentDictionary<Type, ConcurrentDictionary<uint, EcsComponent>> components;
    private static ConcurrentDictionary<uint, EcsEntity> entities;
    private static ConcurrentDictionary<Type, ConcurrentDictionary<uint, ECSFlag>> flags;
    private static readonly ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 32 };

    public static void Init()
    {
        entities = new ConcurrentDictionary<uint, EcsEntity>();
        components = new ConcurrentDictionary<Type, ConcurrentDictionary<uint, EcsComponent>>();
        flags = new ConcurrentDictionary<Type, ConcurrentDictionary<uint, ECSFlag>>();
        systems = new ConcurrentDictionary<Type, ECSSystem>();

        //foreach (var classType in typeof(ECSSystem).Assembly.GetTypes())
        //    if (typeof(ECSSystem).IsAssignableFrom(classType) && !classType.IsAbstract)
        //        systems.TryAdd(classType, Activator.CreateInstance(classType) as ECSSystem);

        systems.TryAdd(typeof(NeuralNetSystem), Activator.CreateInstance(typeof(NeuralNetSystem)) as ECSSystem);
        systems.TryAdd(typeof(BoidRadarSystem), Activator.CreateInstance(typeof(BoidRadarSystem)) as ECSSystem);
        systems.TryAdd(typeof(AlignmentSystem), Activator.CreateInstance(typeof(AlignmentSystem)) as ECSSystem);
        systems.TryAdd(typeof(CohesionSystem),  Activator.CreateInstance(typeof(CohesionSystem)) as ECSSystem);
        systems.TryAdd(typeof(SeparationSystem),Activator.CreateInstance(typeof(SeparationSystem)) as ECSSystem);
        systems.TryAdd(typeof(DirectionSystem), Activator.CreateInstance(typeof(DirectionSystem)) as ECSSystem);
        systems.TryAdd(typeof(ACSSystem),       Activator.CreateInstance(typeof(ACSSystem)) as ECSSystem);
            
        foreach (KeyValuePair<Type, ECSSystem> system in systems) system.Value.Initialize();

        foreach (Type classType in typeof(EcsComponent).Assembly.GetTypes())
            if (typeof(EcsComponent).IsAssignableFrom(classType) && !classType.IsAbstract)
                components.TryAdd(classType, new ConcurrentDictionary<uint, EcsComponent>());

        foreach (Type classType in typeof(ECSFlag).Assembly.GetTypes())
            if (typeof(ECSFlag).IsAssignableFrom(classType) && !classType.IsAbstract)
                flags.TryAdd(classType, new ConcurrentDictionary<uint, ECSFlag>());
    }

    public static void Tick(float deltaTime)
    {
        Parallel.ForEach(systems, parallelOptions, system => { system.Value.Run(deltaTime); });
    }

    public static void RunFlocking(float deltaTime)
    {
        systems[typeof(BoidRadarSystem)].Run(deltaTime);
        systems[typeof(AlignmentSystem)].Run(deltaTime);
        systems[typeof(CohesionSystem)].Run(deltaTime);
        systems[typeof(SeparationSystem)].Run(deltaTime);
        systems[typeof(DirectionSystem)].Run(deltaTime);
    }
    
    public static void RunSystem(float deltaTime, Type systemType)
    {
        systems[systemType].Run(deltaTime);
    }
    
    public static uint CreateEntity()
    {
        entities ??= new ConcurrentDictionary<uint, EcsEntity>();
        EcsEntity ecsEntity = new EcsEntity();
        entities.TryAdd(ecsEntity.GetID(), ecsEntity);
        return ecsEntity.GetID();
    }

    public static void AddSystem(ECSSystem system)
    {
        systems ??= new ConcurrentDictionary<Type, ECSSystem>();

        systems.TryAdd(system.GetType(), system);
    }

    public static void InitSystems()
    {
        foreach (KeyValuePair<Type, ECSSystem> system in systems) system.Value.Initialize();
    }

    public static void AddComponentList(Type component)
    {
        components ??= new ConcurrentDictionary<Type, ConcurrentDictionary<uint, EcsComponent>>();
        components.TryAdd(component, new ConcurrentDictionary<uint, EcsComponent>());
    }

    public static void AddComponent<TComponentType>(uint entityID, TComponentType component)
        where TComponentType : EcsComponent
    {
        component.EntityOwnerID = entityID;
        entities[entityID].AddComponentType(typeof(TComponentType));
        components[typeof(TComponentType)].TryAdd(entityID, component);
    }

    public static bool ContainsComponent<TComponentType>(uint entityID) where TComponentType : EcsComponent
    {
        return entities[entityID].ContainsComponentType<TComponentType>();
    }


    public static IEnumerable<uint> GetEntitiesWithComponentTypes(params Type[] componentTypes)
    {
        ConcurrentBag<uint> matchs = new ConcurrentBag<uint>();
        Parallel.ForEach(entities, parallelOptions, entity =>
        {
            for (int i = 0; i < componentTypes.Length; i++)
                if (!entity.Value.ContainsComponentType(componentTypes[i]))
                    return;

            matchs.Add(entity.Key);
        });
        return matchs;
    }

    public static ConcurrentDictionary<uint, TComponentType> GetComponents<TComponentType>()
        where TComponentType : EcsComponent
    {
        if (!components.ContainsKey(typeof(TComponentType))) return null;

        ConcurrentDictionary<uint, TComponentType> comps = new ConcurrentDictionary<uint, TComponentType>();

        Parallel.ForEach(components[typeof(TComponentType)], parallelOptions,
            component => { comps.TryAdd(component.Key, component.Value as TComponentType); });

        return comps;
    }

    public static TComponentType GetComponent<TComponentType>(uint entityID) where TComponentType : EcsComponent
    {
        return components[typeof(TComponentType)][entityID] as TComponentType;
    }
    
    private static readonly ThreadLocal<Dictionary<Type, (uint[] ids, Array components)>> componentBuffers =
        new(() => new Dictionary<Type, (uint[], Array)>());

    public static (uint[] ids, TComponentType[] components) GetComponentsDirect<TComponentType>() 
        where TComponentType : EcsComponent
    {
        if (!components.TryGetValue(typeof(TComponentType), out ConcurrentDictionary<uint, EcsComponent>? componentDict))
            return (Array.Empty<uint>(), Array.Empty<TComponentType>());

        Dictionary<Type, (uint[] ids, Array components)>? buffers = componentBuffers.Value;
        Type type = typeof(TComponentType);

        if (!buffers.TryGetValue(type, out (uint[] ids, Array components) buffer))
        {
            buffer = (new uint[1024], new TComponentType[1024]);
            buffers[type] = buffer;
        }

        int count = componentDict.Count;
        
        // Fix 1: Handle array resizing with explicit type
        if (buffer.ids.Length < count)
        {
            Array.Resize(ref buffer.ids, count);
            TComponentType[]? tempComponents = (TComponentType[])buffer.components;
            Array.Resize(ref tempComponents, count);
            buffer.components = tempComponents;
        }

        int i = 0;
        TComponentType[]? componentsArray = (TComponentType[])buffer.components;
        foreach (KeyValuePair<uint, EcsComponent> kvp in componentDict)
        {
            buffer.ids[i] = kvp.Key;
            componentsArray[i] = (TComponentType)kvp.Value;
            i++;
        }

        return (
            buffer.ids.Take(count).ToArray(),
            componentsArray.Take(count).ToArray()
        );
    }

    public static void RemoveComponent<TComponentType>(uint entityID) where TComponentType : EcsComponent
    {
        components[typeof(TComponentType)].TryRemove(entityID, out _);
    }

    public static IEnumerable<uint> GetEntitiesWhitFlagTypes(params Type[] flagTypes)
    {
        ConcurrentBag<uint> matchs = new ConcurrentBag<uint>();
        Parallel.ForEach(entities, parallelOptions, entity =>
        {
            for (int i = 0; i < flagTypes.Length; i++)
                if (!entity.Value.ContainsFlagType(flagTypes[i]))
                    return;

            matchs.Add(entity.Key);
        });
        return matchs;
    }

    public static void AddFlag<TFlagType>(uint entityID, TFlagType flag)
        where TFlagType : ECSFlag
    {
        flag.EntityOwnerID = entityID;
        entities[entityID].AddComponentType(typeof(TFlagType));
        flags[typeof(TFlagType)].TryAdd(entityID, flag);
    }

    public static bool ContainsFlag<TFlagType>(uint entityID) where TFlagType : ECSFlag
    {
        return entities[entityID].ContainsFlagType<TFlagType>();
    }

    public static ConcurrentDictionary<uint, TFlagType> GetFlags<TFlagType>() where TFlagType : ECSFlag
    {
        if (!flags.ContainsKey(typeof(TFlagType))) return null;

        ConcurrentDictionary<uint, TFlagType> flgs = new ConcurrentDictionary<uint, TFlagType>();

        Parallel.ForEach(flags[typeof(TFlagType)], parallelOptions,
            flag => { flgs.TryAdd(flag.Key, flag.Value as TFlagType); });

        return flgs;
    }

    public static TFlagType GetFlag<TFlagType>(uint entityID) where TFlagType : ECSFlag
    {
        return flags[typeof(TFlagType)][entityID] as TFlagType;
    }

    public static void RemoveFlag<TFlagType>(uint entityID) where TFlagType : ECSFlag
    {
        flags[typeof(TFlagType)].TryRemove(entityID, out _);
    }

    public static void RemoveEntity(uint agentId)
    {
        entities.TryRemove(agentId, out _);
        foreach (KeyValuePair<Type, ConcurrentDictionary<uint, EcsComponent>> component in components)
            component.Value.TryRemove(agentId, out _);
        foreach (KeyValuePair<Type, ConcurrentDictionary<uint, ECSFlag>> flag in flags)
            flag.Value.TryRemove(agentId, out _);
    }

    public static ECSSystem GetSystem<T>()
    {
        return systems[typeof(T)];
    }
}