using System.Collections.Generic;

namespace RoyTheunissen.Graphing.Utilities
{
    /// <summary>
    /// Generic construction for pooling resources of a certain kind.
    /// </summary>
    public class Pool<ObjectType>
        where ObjectType : class
    {
        private const int DefaultCapacity = 4;
        
        private readonly List<ObjectType> allObjects = new List<ObjectType>();
        public List<ObjectType> AllObjects => allObjects;

        private readonly List<ObjectType> availableObjects = new List<ObjectType>();
        public List<ObjectType> AvailableObjects => availableObjects;

        private readonly List<ObjectType> usedObjects = new List<ObjectType>();
        public List<ObjectType> UsedObjects => usedObjects;

        public int PoolSize => availableObjects.Count + usedObjects.Count;
        
        public delegate ObjectType ObjectCreator();
        private ObjectCreator objectCreator;
        
        public delegate void ObjectDestroyer(ObjectType pooledObject);
        private ObjectDestroyer objectDestroyer;
        
        public delegate void ObjectActivator(ObjectType pooledObject);
        private ObjectActivator objectActivator;
        
        public delegate void ObjectDeactivator(ObjectType pooledObject);
        private ObjectDeactivator objectDeactivator;

        public Pool(
            ObjectCreator objectCreator, ObjectDestroyer objectDestroyer = null,
            ObjectActivator objectActivator = null, ObjectDeactivator objectDeactivator = null,
            int defaultCapacity = DefaultCapacity)
        {
            this.objectCreator = objectCreator;
            this.objectDestroyer = objectDestroyer;
            this.objectActivator = objectActivator;
            this.objectDeactivator = objectDeactivator;

            Grow(defaultCapacity);
        }

        public void Cleanup()
        {
            for (int i = allObjects.Count - 1; i >= 0; i--)
            {
                DestroyPoolObject(allObjects[i]);
            }
            allObjects.Clear();
            usedObjects.Clear();
            availableObjects.Clear();
        }

        private void DestroyPoolObject(ObjectType pooledObject)
        {
            objectDestroyer?.Invoke(pooledObject);
        }

        public ObjectType Get()
        {
            if (availableObjects.Count == 0)
            {
                // Try to grow the pool.
                Grow();
                
                // Still no objects available? Pool wasn't allowed to grow and there's nothing to return.
                if (availableObjects.Count == 0)
                    return null;
            }

            int index = availableObjects.Count - 1;
            ObjectType instance = availableObjects[index];
            
            // If specified, activate the instance first.
            objectActivator?.Invoke(instance);
            
            availableObjects.RemoveAt(index);
            usedObjects.Add(instance);
            
            return instance;
        }

        public void Return(ObjectType objectToReturn)
        {
            ReturnInternal(objectToReturn, true);
        }

        private void ReturnInternal(ObjectType objectToReturn, bool updateLists)
        {
            // If specified, deactivate the instance first.
            objectDeactivator?.Invoke(objectToReturn);

            if (updateLists)
            {
                availableObjects.Add(objectToReturn);
                usedObjects.Remove(objectToReturn);
            }
        }

        public void ReturnAll()
        {
            for (int i = usedObjects.Count - 1; i >= 0; i--)
            {
                ReturnInternal(usedObjects[i], false);
            }
            availableObjects.AddRange(usedObjects);
            usedObjects.Clear();
        }

        private ObjectType CreateObject()
        {
            return objectCreator();
        }

        private void Grow()
        {
            ObjectType newObject = CreateObject();
            
            // If specified, deactivate the instance first.
            objectDeactivator?.Invoke(newObject);
            
            availableObjects.Add(newObject);
            allObjects.Add(newObject);
        }
        
        private void Grow(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                Grow();
            }
        }

        public void EnsureCapacity(int pointCount)
        {
            Grow(pointCount - PoolSize);
        }
    }
}
