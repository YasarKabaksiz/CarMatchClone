using CarMatchClone.Data;
using UnityEngine;

namespace CarMatchClone.Core.Events
{
    [CreateAssetMenu(menuName = "CarMatchClone/Events/BoosterCountEventChannel")]
    public class BoosterCountEventChannel : GameEventChannel<BoosterCountPayload> { }
}
