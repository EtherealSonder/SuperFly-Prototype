using UnityEngine;

public class FlyingMode : IPlayerMode
{
    public Vector3 SimulateMovement(
        float dirX, float dirZ,
        float moveSpeed, float acceleration, float velPower,
        float cameraAngle, float playerAngle,
        float turnSmoothTime,
        ref float turnSmoothVelocity,
        ref float rotateAngle,
        Vector3 velocity)
    {
        Vector3 result;

        if (Mathf.Abs(dirX) > 0f || Mathf.Abs(dirZ) > 0f)
        {
            // Determine direction relative to camera
            float targetAngle = Mathf.Atan2(dirX, dirZ) * Mathf.Rad2Deg + cameraAngle;
            float angle = Mathf.SmoothDampAngle(playerAngle, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            rotateAngle = angle;

            Vector3 direction = Quaternion.Euler(0.0f, targetAngle, 0.0f) * Vector3.forward;

            // Apply smooth flying acceleration
            Vector3 targetVelocity = direction.normalized * moveSpeed;
            Vector3 velocityDiff = targetVelocity - velocity;

            float accelRate = acceleration;

            result = new Vector3(
                Mathf.Pow(Mathf.Abs(velocityDiff.x) * accelRate, velPower) * Mathf.Sign(velocityDiff.x),
                0.0f,
                Mathf.Pow(Mathf.Abs(velocityDiff.z) * accelRate, velPower) * Mathf.Sign(velocityDiff.z)
            );
        }
        else
        {
            result = Vector3.zero;
        }

        return result;
    }
}
