using GyroCue.Practice;
using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Shared 3D practice mapping. Phone heading controls XZ aim and forward
    /// acceleration controls power; tip offset and cue elevation remain on the
    /// game phone's touch widgets for predictable casual play.
    /// </summary>
    public static class RemotePracticeCueMapping
    {
        public static Vector3 ToTableAim(Vector2 remoteAim)
        {
            var aim = new Vector3(remoteAim.x, 0f, remoteAim.y);
            return aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector3.forward;
        }

        public static CueStrokeSample ToPracticeStroke(ShotCommand command, Vector2 touchStrikeOffset)
        {
            return new CueStrokeSample(
                command.NormalizedPower,
                new Vector2(
                    Mathf.Clamp(touchStrikeOffset.x, -1f, 1f),
                    Mathf.Clamp(touchStrikeOffset.y, -1f, 1f)),
                command.NormalizedPower * 9f);
        }
    }
}
