using UnityEngine;
using Unity.Netcode;

public class NetworkAnimator : NetworkBehaviour {
    private Animator animator;

    public override void OnNetworkSpawn() {
        animator = GetComponent<Animator>();
    }

    public void SetAnimation(string parameter, float value) {
        if (IsClient) {
            animator.SetFloat(parameter, value);
            AnimationServerRpc(parameter, value);
        } else {
            AnimationClientRpc(parameter, value);
        }
    }

    public void SetAnimation(string parameter, int value) {
        if (IsClient) {
            animator.SetInteger(parameter, value);
            AnimationServerRpc(parameter, value);
        } else {
            AnimationClientRpc(parameter, value);
        }
    }

    public void SetAnimation(string parameter, bool value) {
        if (IsClient) {
            animator.SetBool(parameter, value);
            AnimationServerRpc(parameter, value);
        } else {
            AnimationClientRpc(parameter, value);
        }
    }

    public void SetAnimation(string trigger) {
        if (IsClient) {
            animator.SetTrigger(trigger);
            AnimationServerRpc(trigger);
        } else {
            AnimationClientRpc(trigger);
        }
    }

    [ServerRpc]
    public void AnimationServerRpc(string parameter, float value) {
        AnimationClientRpc(parameter, value, Util.GetClientRpcParamsExcept(OwnerClientId));
    }

    [ServerRpc]
    public void AnimationServerRpc(string parameter, int value) {
        AnimationClientRpc(parameter, value, Util.GetClientRpcParamsExcept(OwnerClientId));
    }

    [ServerRpc]
    public void AnimationServerRpc(string parameter, bool value) {
        AnimationClientRpc(parameter, value, Util.GetClientRpcParamsExcept(OwnerClientId));
    }

    [ServerRpc]
    public void AnimationServerRpc(string trigger) {
        AnimationClientRpc(trigger, Util.GetClientRpcParamsExcept(OwnerClientId));
    }

    [ClientRpc]
    public void AnimationClientRpc(string parameter, float value, ClientRpcParams clientRpcParams = default) {
        animator.SetFloat(parameter, value);
    }

    [ClientRpc]
    public void AnimationClientRpc(string parameter, int value, ClientRpcParams clientRpcParams = default) {
        animator.SetInteger(parameter, value);
    }

    [ClientRpc]
    public void AnimationClientRpc(string parameter, bool value, ClientRpcParams clientRpcParams = default) {
        animator.SetBool(parameter, value);
    }

    [ClientRpc]
    public void AnimationClientRpc(string trigger, ClientRpcParams clientRpcParams = default) {
        animator.SetTrigger(trigger);
    }
}
