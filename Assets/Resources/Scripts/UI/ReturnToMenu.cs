using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviourPunCallbacks
{
    private string sceneName;

    public static void Go(string menuSceneName)
    {
        if (FindAnyObjectByType<ReturnToMenu>() != null)
            return;

        GameObject go = new GameObject("ReturnToMenu");
        DontDestroyOnLoad(go);
        ReturnToMenu returner = go.AddComponent<ReturnToMenu>();
        returner.sceneName = menuSceneName;
        returner.Begin();
    }

    private void Begin()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        else
            Finish();
    }

    public override void OnLeftRoom()
    {
        Finish();
    }

    private void Finish()
    {
        SceneManager.LoadScene(sceneName);
        Destroy(gameObject);
    }
}