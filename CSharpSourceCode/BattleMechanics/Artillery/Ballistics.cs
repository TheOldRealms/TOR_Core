using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;

namespace TOR_Core.BattleMechanics.Artillery
{
    public static class Ballistics
    {
        public static int GetLaunchVectorForProjectileToHitTarget(Vec3 startPosition, float speed, Vec3 targetPosition, out Vec3 lowSolution, out Vec3 highSolution)
        {
            lowSolution = Vec3.Zero;
            highSolution = Vec3.Zero;
            int numSolutions = 0;

            Vec3 diff = targetPosition - startPosition;
            Vec3 diffXY = new Vec3(diff.X, diff.Y, 0);
            float groundDistance = diffXY.Length;

            float speed2 = speed * speed;
            float speed4 = speed * speed * speed * speed;
            float heightDiff = diff.z;
            float gravity = MBGlobals.Gravity;

            float root = speed4 - gravity * (gravity * groundDistance * groundDistance + 2 * heightDiff * speed2);
            if (root < 0) return 0;

            root = Mathf.Sqrt(root);
            float lowAng = Mathf.Atan2(speed2 - root, gravity * groundDistance);
            float highAng = Mathf.Atan2(speed2 + root, gravity * groundDistance);
            numSolutions = lowAng != highAng ? 2 : 1;

            Vec3 groundDir = diffXY.NormalizedCopy();
            lowSolution = groundDir * Mathf.Cos(lowAng) * speed + Vec3.Up * Mathf.Sin(lowAng) * speed;
            if (numSolutions > 1)
                highSolution = groundDir * Mathf.Cos(highAng) * speed + Vec3.Up * Mathf.Sin(highAng) * speed;

            return numSolutions;
        }

        public static float GetMaximumRange(float speed, float height_diff)
        {
            float angle = 45 * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float gravity = MBGlobals.Gravity;
            float range = (speed * cos / gravity) * (speed * sin + Mathf.Sqrt(speed * speed * sin * sin + 2 * gravity * height_diff));
            return range;
        }
        public static float GetTimeOfProjectileFlight(float velocity, float angle, float distance)
        {
            return distance / (velocity * Mathf.Cos(angle));
        }
    }
}
