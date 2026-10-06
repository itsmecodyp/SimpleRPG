using System.Collections.Generic;
using UnityEngine;

namespace SimpleRPG
{
    public class NarrativeManager
    {
        private readonly Queue<NarrativeEvent> queue = new Queue<NarrativeEvent>();

        private NarrativeEvent currentEvent;
        private float timer;
        public bool flashTiles = false;

        public bool IsPlaying => currentEvent != null;

        public void QueueEvent(NarrativeEvent e)
        {
            queue.Enqueue(e);

            if (currentEvent == null)
            {
                StartNextEvent();
            }
        }

        public void QueueEvents(params NarrativeEvent[] events)
        {
            foreach (var e in events)
                queue.Enqueue(e);

            if (currentEvent == null)
            {
                StartNextEvent();
            }
        }

        public void Update()
        {
            if (currentEvent == null)
                return;

            timer -= Time.deltaTime;
            if (flashTiles && currentEvent.targetTile != null) {
                currentEvent.FlashTile();
            } 
            if (timer <= 0f)
            {
                currentEvent.EndEvent();
                StartNextEvent();
            }
        }

        private void StartNextEvent()
        {
            if (queue.Count == 0)
            {
                currentEvent = null;
                return;
            }

            currentEvent = queue.Dequeue();
            timer = currentEvent.duration;
            currentEvent.StartEvent();
        }

        public void Clear()
        {
            queue.Clear();

            if (currentEvent != null)
                currentEvent.EndEvent();

            currentEvent = null;
        }
    }
}