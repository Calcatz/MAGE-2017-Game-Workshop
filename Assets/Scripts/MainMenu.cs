using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MainMenu : MonoBehaviour {

	public void HostGame() {
        NetworkManager.singleton.StartHost();
    }

    public void JoinGame() {
        NetworkManager.singleton.StartClient();
    }

}
