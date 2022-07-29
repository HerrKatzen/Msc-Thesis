using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VesselSimulator.Util;

namespace VesselSimulator.UI
{
    public class PopUpWithButton : Singleton<PopUpWithButton>
    {
        [SerializeField]
        private GameObject popupGameobject;
        [SerializeField]
        private TextMeshProUGUI popupText;

        public void PopupText(string text)
        {
            popupText.text = text;
            popupGameobject.SetActive(true);
        }

        public void OK()
        {
            popupGameobject.SetActive(false);
        }
    }
}