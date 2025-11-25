using Soulstone.Datamodels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Managers
{
    internal class DiceSystemManager
    {
        private static DiceSystemManager? instance = null;

        private DiceSystem currentDiceSystem;

        private DiceSystemManager()
        {
            // Private constructor to prevent instantiation
        }

        public static DiceSystemManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DiceSystemManager();
                }
                return instance;
            }
        }

        internal DiceSystem CurrentDiceSystem { get => currentDiceSystem; set => currentDiceSystem = value; }

        public void Init()
        {
            currentDiceSystem = DiceSystem.LoadDiceSystem("Standard_Dice_System");
        }
    }
}
