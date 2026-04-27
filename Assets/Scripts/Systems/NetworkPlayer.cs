using UnityEngine;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;

public sealed class NetworkPlayer : MonoBehaviourPun, IPunObservable
#else
public sealed class NetworkPlayer : MonoBehaviour
#endif
{
#if PHOTON_UNITY_NETWORKING
    [SerializeField] private float remoteLerpSpeed = 12f;
#endif

    private Vector3 _networkPosition;
    private Quaternion _networkRotation = Quaternion.identity;

    private void Awake()
    {
        _networkPosition = transform.position;
        _networkRotation = transform.rotation;
    }

    private void Update()
    {
#if PHOTON_UNITY_NETWORKING
        if (photonView.IsMine)
        {
            return;
        }

        transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * remoteLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation, Time.deltaTime * remoteLerpSpeed);
#endif
    }

#if PHOTON_UNITY_NETWORKING
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            return;
        }

        _networkPosition = (Vector3)stream.ReceiveNext();
        _networkRotation = (Quaternion)stream.ReceiveNext();
    }
#endif
}
