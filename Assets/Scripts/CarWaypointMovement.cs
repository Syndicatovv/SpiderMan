using UnityEngine;
public class CarWaypointMovements : MonoBehaviour
{
    public Transform[] waypoints;
    public Transform[] wheels;
    public float speed = 8f;
    public float rotationSpeed = 5f;

    private int currentWaypointIndex = 0;

    // Update is called once per frame
    void Update()
    {
        if (waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);
        
        // Вычисляем вектор (направление) от машины до точки
        Vector3 direction = targetWaypoint.position - transform.position;

        // Проверяем, что направление не нулевое (чтобы машину не дергало, когда она уже стоит на точке)
        if(direction != Vector3.zero)
        {
            // LookRotation определяет, под каким углом машина должна быть повернута, чтобы смотреть строго на точку
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Slerp плавно вращает машину от текущего поворота к идеальному со скоростью rotationSpeed
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Vector3.Distance измеряет расстояние между машиной и точкой в метрах.
        // Если до точки осталось меньше 30 сантиметров (0.3f), считаем, что приехали!

        // Знак % (остаток от деления) — это красивый трюк программистов. 
        // Он прибавляет к индексу единицу (0, 1, 2...), но как только мы дойдем до конца списка (до 10-й точки), он автоматически сбросит номер обратно на 0
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.3f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        if (wheels != null && wheels.Length > 0)
        {
            foreach(Transform wheel in wheels)
            {
                if (wheel != null)
                {
                    // Rotate — это встроенная команда Unity, которая крутит объект.
                    // Vector3.right означает, что крутить мы будем вокруг оси X (вперёд-назад, как катится колесо).
                    // 200f — это скорость вращения колеса (дробное число).
                    // Time.deltaTime делает вращение плавным и независимым от FPS.
                    // Space.Self говорит, что колесо должно крутиться вокруг своей собственной оси, а не вокруг центра машины.
                    wheel.Rotate(Vector3.right * 200f * Time.deltaTime, Space.Self);
                }
            }
        }
    }
}
