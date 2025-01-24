using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPlayerController : MonoBehaviour
{
    public RawImage rawImage;    // RawImage ที่แสดงวิดีโอ
    public VideoPlayer videoPlayer; // VideoPlayer ที่เล่นวิดีโอ

    void Start()
    {
        // ตั้งค่า VideoPlayer
        videoPlayer.renderMode = VideoRenderMode.APIOnly; // ตั้งให้ VideoPlayer ไม่แสดงผลเอง
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare(); // เตรียมตัวเล่นวิดีโอ
    }
    
    private void OnVideoPrepared(VideoPlayer vp)
    {
        rawImage.texture = videoPlayer.texture; // ตั้ง RawImage ให้แสดงผลจาก VideoPlayer
        videoPlayer.Play(); // เริ่มเล่นวิดีโอ
    }
}