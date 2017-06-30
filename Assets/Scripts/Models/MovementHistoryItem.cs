using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class MovementHistoryItem
    {
        public float xPosition;
        public float yPosition;
        public float zPosition;
        public float frame;
        public InputFrame inputFrame;
    }
}
