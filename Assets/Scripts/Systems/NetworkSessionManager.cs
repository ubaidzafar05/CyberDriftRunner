using UnityEngine;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;

public sealed class NetworkSessionManager : MonoBehaviourPunCallbacks
#else
public sealed class NetworkSessionManager : MonoBehaviour
#endif
{
    public static NetworkSessionManager Instance { get; private set; }

    [SerializeField] private bool connectOnStart;
    [SerializeField] private byte maxPlayersPerRoom = 4;

    public NetworkConnectionState State { get; private set; } = NetworkConnectionState.Offline;
    public bool SupportsRealtime
    {
        get
        {
#if PHOTON_UNITY_NETWORKING
            return true;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (connectOnStart)
        {
            ConnectAndQueue();
        }
    }

    public void ConnectAndQueue()
    {
        if (LiveOpsSystem.Instance != null && !LiveOpsSystem.Instance.CurrentConfig.RealtimeMultiplayerEnabled)
        {
            SetState(NetworkConnectionState.Offline);
            return;
        }

#if PHOTON_UNITY_NETWORKING
        SetState(NetworkConnectionState.Connecting);
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.ConnectUsingSettings();
#else
        Debug.Log($"[NetworkSessionManager] Photon PUN is not installed. Falling back to ghost mode. Desired room size: {maxPlayersPerRoom}");
        SetState(NetworkConnectionState.Offline);
#endif
    }

#if PHOTON_UNITY_NETWORKING
    public override void OnConnectedToMaster()
    {
        SetState(NetworkConnectionState.Matchmaking);
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayersPerRoom };
        PhotonNetwork.CreateRoom(null, options);
    }

    public override void OnJoinedRoom()
    {
        SetState(NetworkConnectionState.InRoom);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[NetworkSessionManager] Disconnected: {cause}");
        SetState(NetworkConnectionState.Failed);
    }
#endif

    private void SetState(NetworkConnectionState nextState)
    {
        State = nextState;
        EventBus.Publish(new NetworkStateChangedEvent(State, SupportsRealtime));
    }
}
