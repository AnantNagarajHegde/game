using UnityEngine;

namespace Paridhwani
{
    // Compose the existing world only on the title. Never move the player or save a pose.
    [DefaultExecutionOrder(100)]
    public sealed class VillageTitleBackdrop : MonoBehaviour
    {
        TeaStallSession session;
        public void Bind(TeaStallSession value) => session=value;
        void LateUpdate()
        {
            if(!session || !session.ui || session.ui.Mode!="main-menu" || !session.vendor || !session.player.view)return;
            var view=session.player.view;
            var stall=session.vendor.position;
            // Open lane, facing the tea stall with room for the title column at left.
            var position=stall+new Vector3(-2.7f,2.5f,-9.5f);
            var target=stall+new Vector3(-3.8f,1.4f,1.5f);
            view.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            view.fieldOfView=55;
        }
    }
}
