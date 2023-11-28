using System;
using UnityEngine;
using UnityEngine.Profiling;
using ROS2;

public class ROSCamera : MonoBehaviour
{
    public Camera mainCamera;
    public ROS2UnityComponent ros2Unity;
    public int textureWidth = 848;
    public int textureHeight = 480;
    public int publishFps = 1;

    private RenderTexture renderTexture;
    private ROS2Node ros2Node;
    private IPublisher<sensor_msgs.msg.Image> colorFramePub;
    private IPublisher<sensor_msgs.msg.Image> depthFramePub;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
            mainCamera.depthTextureMode = DepthTextureMode.Depth;
        }

        renderTexture = new RenderTexture(textureWidth, textureHeight, 16);
        mainCamera.targetTexture = renderTexture;

        if (ros2Unity.Ok())
        {
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("ROS2UnityCameraNode");
                colorFramePub = ros2Node.CreatePublisher<sensor_msgs.msg.Image>("color_frame");
                depthFramePub = ros2Node.CreatePublisher<sensor_msgs.msg.Image>("depth_frame");
            }
        }

        float interval_secs = 1.0f / publishFps;
        InvokeRepeating("PublishFrame", interval_secs, interval_secs);
    }

    private void PublishFrame()
    {
        sensor_msgs.msg.Image colorImageMsg = ConvertToImageMsg(GetColorFrame());
        sensor_msgs.msg.Image depthImageMsg = ConvertToImageMsg(GetDepthFrame());
        colorFramePub.Publish(colorImageMsg);
        depthFramePub.Publish(depthImageMsg);
    }

    private Texture2D GetColorFrame()
    {
        // Texture2D.ReadPixels looks at the active RenderTexture.
        RenderTexture oldActiveRenderTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D frame = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        frame.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);

        // Restore active RT
        RenderTexture.active = oldActiveRenderTexture;

        return frame;
    }

    private Texture2D GetDepthFrame()
    {
        // Texture2D.ReadPixels looks at the active RenderTexture.
        RenderTexture oldActiveRenderTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D frame = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.R16, false);
        frame.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);

        // Restore active RT
        RenderTexture.active = oldActiveRenderTexture;

        return frame;
    }

    private sensor_msgs.msg.Image ConvertToImageMsg(Texture2D frame)
    {
        if (frame.format != TextureFormat.RGB24 && frame.format != TextureFormat.R16)
        {
            throw new ArgumentException($"Unsupported texture format: {frame.format}");
        }

        // Unity's texture coordinates have origin at bottom left with OpenGL, so we need to
        // flip the pixels vertically
        if (!SystemInfo.graphicsUVStartsAtTop)
        {
            FlipVertically(frame);
        }

        bool isColor = frame.format == TextureFormat.RGB24;
        uint width = unchecked((uint)frame.width);
        uint height = unchecked((uint)frame.height);

        sensor_msgs.msg.Image imageMsg = new sensor_msgs.msg.Image
        {
            Width = width,
            Height = height,
            Encoding = isColor ? "rgb8" : "mono16",
            Data = frame.GetRawTextureData(),
            Step = isColor ? width * 3 : width * 2
        };
        TimeSpan timeSinceEpoch = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        imageMsg.UpdateHeaderTime((int)timeSinceEpoch.TotalSeconds, unchecked((uint)(timeSinceEpoch.TotalMilliseconds * 1e6 % 1e9)));

        return imageMsg;
    }

    private void FlipVertically(Texture2D frame)
    {
        Profiler.BeginSample("FlipVertically");
        Color32[] originalPixels = frame.GetPixels32();
        // Use a temporary buffer to store flipped pixels
        Color32[] flippedPixels = new Color32[frame.width * frame.height];
        for (int y = 0; y < frame.height; y++)
        {
            int index = y * frame.width;
            int flippedIndex = (frame.height - 1 - y) * frame.width;

            // Copy the pixels to the temporary buffer
            Array.Copy(originalPixels, index, flippedPixels, flippedIndex, frame.width);
        }
        frame.SetPixels32(flippedPixels);
        Profiler.EndSample();
    }
}
