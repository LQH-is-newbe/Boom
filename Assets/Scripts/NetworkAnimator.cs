using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkAnimator : NetworkBehaviour {
    private Animator animator;

    private void Start() {
        animator = GetComponent<Animator>();
    }

    public void SetAnimation(string parameter, float value) {
        animator.SetFloat(parameter, value);
        AnimationServerRpc(parameter, value, NetworkManager.Singleton.LocalClientId);
    }

    public void SetAnimation(string parameter, int value) {
        animator.SetInteger(parameter, value);
        AnimationServerRpc(parameter, value, NetworkManager.Singleton.LocalClientId);
    }

    public void SetAnimation(string parameter, bool value) {
        animator.SetBool(parameter, value);
        AnimationServerRpc(parameter, value, NetworkManager.Singleton.LocalClientId);
    }

    public void SetAnimation(string trigger) {
        animator.SetTrigger(trigger);
        AnimationServerRpc(trigger, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    public void AnimationServerRpc(string parameter, float value, ulong clientId) {
        AnimationClientRpc(parameter, value, Util.GetClientRpcParamsExcept(clientId));
    }

    [ServerRpc]
    public void AnimationServerRpc(string parameter, int value, ulong clientId) {
        AnimationClientRpc(parameter, value, Util.GetClientRpcParamsExcept(clientId));
    }

    [ServerRpc]
    public void AnimationServerRpc(string parameter, bool value, ulong clientId) {
        AnimationClientRpc(parameter, value, Util.GetClientRpcParamsExcept(clientId));
    }

    [ServerRpc]
    public void AnimationServerRpc(string trigger, ulong clientId) {
        AnimationClientRpc(trigger, Util.GetClientRpcParamsExcept(clientId));
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
