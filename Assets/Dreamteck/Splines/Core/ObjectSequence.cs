using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines
{
    [System.Serializable]
    public class ObjectSequence<T>
    {
        public T startObject;
        public T endObject;
        public T[] objects;
        public enum Iteration { Ordered, Random }
        public Iteration iteration = Iteration.Ordered;
        public int randomSeed
        {
            get { return _randomSeed; }
            set
            {
                if (value != _randomSeed)
                {
                    _randomSeed = value;
                    randomizer = new System.Random(_randomSeed);
                }
            }
        }
        [SerializeField]
        [HideInInspector]
        private int _randomSeed = 1;
        [SerializeField]
        [HideInInspector]
        private int index = 0;
        [SerializeField]
        [HideInInspector]
        System.Random randomizer;
        
        public ObjectSequence(){
            randomizer = new System.Random(_randomSeed);
        }

        public T GetFirst()
        {
            if (startObject != null) return startObject;
            else return Next();
        }

        public T GetLast()
        {
            if (endObject != null) return endObject;
            else return Next();
        }

        public T Next()
        {
            if (iteration == Iteration.Ordered)
            {
                if (index >= objects.Length) index = 0;
                return objects[index++];
            } else
            {
                int randomIndex = randomizer.Next(objects.Length-1);
                return objects[randomIndex];
            }
        }
    }
}