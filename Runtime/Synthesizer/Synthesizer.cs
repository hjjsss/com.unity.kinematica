using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    /// <summary>
    /// The motion synthesizer is the actual core component of Kinematica.
    /// </summary>
    /// <remarks>
    /// The motion synthesizer represents the actual core implementation
    /// of Kinematica which can be used in a pure DOTS environment directly.
    /// <para>
    /// It provides a raw transform buffer which represents the current
    /// character pose and does not provide any infrastructure to feed
    /// the current pose to the character.
    /// </para>
    /// <para>
    /// The motion synthesizer continuously "plays" an sequence of animation
    /// poses. The current time can be changed by "pushing" a new time to
    /// the synthesizer. Upon pushing a new time, the synthesizer automatically
    /// removes any deviations between the previous pose and the new pose.
    /// </para>
    /// <para>
    /// The motion synthesizer also has a convenient method of creating
    /// semantic queries, which filter the poses stored in the motion library
    /// based on tags and markers.
    /// </para>
    /// <para>
    /// Last but not least, the motion synthesizer contains a task graph
    /// that consist of a hierarchy of nodes that collectively can operate
    /// on a sequence of poses to perform a user defined processing step
    /// in order to arrive at a single new "push time" each frame.
    /// </para>
    /// </remarks>
    /// <seealso cref="MotionSynthesizer.Push"/>
    /// <seealso cref="MotionSynthesizer.Query"/>
    [BurstCompile]
    public partial struct MotionSynthesizer
    {
        internal static unsafe MemoryHeader<MotionSynthesizer> Create(BinaryReference binaryRef, AffineTransform worldRootTransform, float blendDuration)
        {
            var blobAsset = binaryRef.LoadBinary();

            var memoryRequirements = GetMemoryRequirements(ref blobAsset.Value, blendDuration);

            MemoryHeader<MotionSynthesizer> synthesizer;

            var memoryBlock = MemoryBlock.Create(
                memoryRequirements, Allocator.Persistent, out synthesizer);

            synthesizer.Ref.Construct(ref memoryBlock,
                blobAsset, worldRootTransform, blendDuration);

            Assert.IsTrue(memoryBlock.IsComplete);

            return synthesizer;
        }

        internal MemoryRef<MemoryChunk> MemoryChunk => memoryChunk;

        /// <summary>
        /// Allows direct access to the underlying Kinematica runtime asset.
        /// </summary>
        public ref Binary Binary
        {
            get => ref m_binary.Value;
        }

        /// <summary>
        /// Update method that needs to be called each frame
        /// to advance the state of the motion synthesizer.
        /// </summary>
        /// <remarks>
        /// In a game object environment this method gets automatically
        /// called from the Kinematica component.
        /// </remarks>
        /// <param name="deltaTime">Delta time in seconds.</param>
        /// <seealso cref="Unity.Kinematica.Kinematica"/>
        public bool Update(float deltaTime)
        {
            rootDeltaTransform = AffineTransform.identity;

            if (frameCount < 0 || lastProcessedFrameCount == frameCount)
            {
                return false;
            }
            lastProcessedFrameCount = frameCount;

            Assert.IsTrue(deltaTime > 0.0f);

            _deltaTime = deltaTime;

            UpdateTaskGraph();

            UpdateTime(deltaTime);

            if (Time.IsValid)
            {
                rootDeltaTransform =
                    GetTrajectoryDeltaTransform(deltaTime);

                rootTransform *= rootDeltaTransform;
                rootTransform.q = math.normalize(rootTransform.q);

                trajectory.Update(rootTransform,
                    rootDeltaTransform, deltaTime);

                poseGenerator.Update(
                    rootTransform, samplingTime,
                    deltaTime);
            }

            return Time.IsValid;
        }

        internal static MemoryRequirements GetMemoryRequirements(ref Binary binary, float blendDuration)
        {
            var memoryRequirements = MemoryRequirements.Of<MotionSynthesizer>();

            memoryRequirements += DataType.GetMemoryRequirements();

            memoryRequirements += TraitType.GetMemoryRequirements(ref binary);

            memoryRequirements += PoseGenerator.GetMemoryRequirements(ref binary, blendDuration);

            memoryRequirements += TrajectoryModel.GetMemoryRequirements(ref binary);

            return memoryRequirements;
        }
    }
}
