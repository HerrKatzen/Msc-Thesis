using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LosPathFollower
{
    public static PathCommand GetPathCommand(Vector2 NE, List<Vector2> waypoints, int currentWaypointIDX, float shipLenght, float nShipLenghts = 2f, float distThreshod = -1f)
    {
        if (distThreshod == -1f) distThreshod = 2f * shipLenght;
        Vector2 currWP = waypoints[currentWaypointIDX];
        Vector2 prevWP = currentWaypointIDX > 0 ? waypoints[currentWaypointIDX - 1] : new Vector2(NE.x, NE.y);

        if (Vector2.Distance(currWP, NE) < distThreshod)
        {
            if(waypoints.Count > currentWaypointIDX + 1)
            {
                prevWP = waypoints[currentWaypointIDX];
                currentWaypointIDX++;
                currWP = waypoints[currentWaypointIDX];
            }
        }
        //Calculate circle of acceptance
        var acceptRad = shipLenght * nShipLenghts;

        //Calculate the angle between the previous and current waypoints
        var delta_pos = currWP - prevWP;
        var path_angle = Mathf.Atan2(delta_pos.y, delta_pos.x);

        //Calculate the cross track error, i.e. the projected distance from the vessel to the line segment
        var cross_track_error = - (NE.x - prevWP.x) * Mathf.Sin(path_angle)
                                + (NE.y - prevWP.y) * Mathf.Cos(path_angle);

        var vel_angle = Mathf.Atan(-cross_track_error / Mathf.Sqrt(acceptRad * acceptRad + cross_track_error * cross_track_error));
        PathCommand command = new PathCommand();
        command.headingCommand = Mathf.Rad2Deg * (path_angle + vel_angle);
        command.waypointIndex = currentWaypointIDX;
        return command;
    }

    public struct PathCommand
    {
        public float headingCommand;
        public int waypointIndex;
    }
}
