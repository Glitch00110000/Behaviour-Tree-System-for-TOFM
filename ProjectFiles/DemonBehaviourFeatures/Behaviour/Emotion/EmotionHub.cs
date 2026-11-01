using System;
using System.Collections.Generic;

namespace Perception
{
    // Do kosa cele
    public enum EmotionStatus
    {
        Hunting,
        Afraid,
        Playful,
        Frustrated,
        LittleBitch,
    }
    
    public class EmotionHub
    {
        private Dictionary<EmotionStatus,emotionSettings> emotionDictionary = new Dictionary<EmotionStatus,emotionSettings>();


        public EmotionHub()
        {
            throw  new NotImplementedException();
        }


        public EmotionStatus Process()
        {
            foreach (var keyValuePair in emotionDictionary)
            {
                keyValuePair.Value.Decay();
            }
            
            return EmotionStatus.Hunting;
        }
        
        private struct emotionSettings
        {
            private readonly int priority;
            private readonly float maxIrritation;
            private float currentIrritation;
            private readonly float decayRate;

            public emotionSettings(int priority, float maxIrritation, float decayRate)
            {
                this.priority = priority;
                this.maxIrritation = maxIrritation;
                this.decayRate = decayRate;
                currentIrritation = 0;
            }
            
            public  float CurrentIrration
            {
                get => currentIrritation;
            }
            
            public int Priority => priority;

            public void Iritate()
            {
                currentIrritation = Math.Clamp(currentIrritation + 1, 0, maxIrritation);
            } 
            
            public void Decay()
            {
                currentIrritation = Math.Clamp(currentIrritation - decayRate , 0, maxIrritation);;
            }
            
            
        }
        
    }
}