using Unity.SnapshotDebugger;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    internal struct AnimationFrameInfo
    {
        public float    endTime;
        public float    weight;

        public float    animFrame;
        public float    animTime;
    }

    internal class AnimationRecord
    {
        public int                              sequenceIdentifier;
        public string                           animName;

        public float                            startTime;
        public float                            endTime;

        public int                              rank;

        public CircularList<AnimationFrameInfo> animFrames;


        public void AddAnimationFrame(float endTime, float weight, float animFrame, float animTime)
        {
            animFrames.PushBack(new AnimationFrameInfo()
            {
                endTime = endTime,
                weight = weight,
                animFrame = animFrame,
                animTime = animTime
            });

            Assert.IsTrue(endTime <= this.endTime);
        }

        public void PruneAnimationFramesBeforeTimestamp(float timestamp)
        {
            while (animFrames.Count > 0 && animFrames[0].endTime < timestamp)
            {
                startTime = timestamp;
                animFrames.PopFront();
            }
        }
    }
}
