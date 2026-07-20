
using UnityEngine;

namespace StorytellersTable.UiLogic
{
    public abstract class CustomUIComponent : MonoBehaviour
    {
        private void Awake()
        {
            Init();
        }
        public void Init()
        {
            Setup();
            Configure();
        }

        public abstract void Setup();

        public abstract void Configure();


    }

}
