using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class LightningPathController : MonoBehaviour
{
    public Transform[] pathPoints; // Các điểm của hình vuông (hoặc đường đi)
    public float runSpeed = 0.1f;  // Thời gian delay giữa mỗi đoạn (tạo cảm giác tia sét chạy)
    public float lightningThickness = 1f; // Độ dày của tia sét

    private ParticleSystem pSystem;

    void Start()
    {
        pSystem = GetComponent<ParticleSystem>();
        StartCoroutine(RunLightningArc());
    }

    IEnumerator RunLightningArc()
    {
        // Chạy lặp lại liên tục
        while (true)
        {
            for (int i = 0; i < pathPoints.Length; i++)
            {
                Vector3 startPoint = pathPoints[i].position;
                // Nếu là điểm cuối thì nối về điểm đầu để tạo hình vuông khép kín
                Vector3 endPoint = pathPoints[(i + 1) % pathPoints.Length].position; 

                EmitLightningSegment(startPoint, endPoint);

                // Đợi một chút rồi mới bắn đoạn tiếp theo tạo cảm giác "chạy dọc"
                yield return new WaitForSeconds(runSpeed);
            }
            yield return new WaitForSeconds(0.5f); // Nghỉ một chút trước khi chạy vòng mới
        }
    }

    void EmitLightningSegment(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        Vector3 midPoint = start + (direction / 2f);

        // Tính góc xoay cho 2D (Mặt phẳng XY)
        // Nếu game của bạn là mặt phẳng XZ (Top-down 3D), thay direction.y bằng direction.z
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Tạo thông số cho hạt
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        
        // Vị trí nằm ngay giữa 2 điểm
        emitParams.position = midPoint; 
        
        // Kéo dài hạt theo trục X bằng đúng khoảng cách 2 điểm, trục Y là độ dày
        emitParams.startSize3D = new Vector3(distance, lightningThickness, 1f); 
        
        // Xoay hạt dọc theo đường nối 2 điểm
        emitParams.rotation3D = new Vector3(0, 0, angle); 

        // Bắn ra đúng 1 hạt
        pSystem.Emit(emitParams, 1);
    }
}