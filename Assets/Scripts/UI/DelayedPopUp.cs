using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VesselSimulator.Util;

namespace VesselSimulator.UI
{
    public class DelayedPopUp : Singleton<DelayedPopUp>
    {
        [SerializeField]
        private GameObject popupGameobject;
        [SerializeField]
        private TextMeshProUGUI popupText;

        public async void PopupTextWithDelay(string text, int delay = 2000)
        {
            popupText.text = text;
            popupGameobject.SetActive(true);
            await Task.Delay(delay);
            popupGameobject.SetActive(false);
        }
    }
}