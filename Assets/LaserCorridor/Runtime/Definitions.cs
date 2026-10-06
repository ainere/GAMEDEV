using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaserCorridor
{
    public enum RunState { Ready, Running, ExitOpen, Dead, Won }
    public enum PanelEffect { None, Healing, Shield, LaserSlow, MovementSlow, Shock }
    public enum DodgePose { Stand, Crouch, Jump }
    public enum HitRegion { Legs, Chest, Head }
    public enum PatternFamily { LowJump, CrouchBeam, VerticalCurtain, DiagonalSlash, OffsetPairs, Cross, Star, Zigzag, Chevron, Square, Diamond, Circle, CornerGrid, OffsetGrid, CircleGrid, Comb }

    [Serializable]
    public struct BeamSegment
    {
        public Vector2 a, b;
        public BeamSegment(Vector2 start, Vector2 end) { a=start; b=end; }
    }

    public struct FloorHazard
    {
        public Vector2 center;
        public float halfSize;
        public FloorHazard(Vector2 point, float half) { center=point; halfSize=half; }
    }

    public sealed class WavePlan
    {
        public int id;
        public PatternFamily family;
        public int variant;
        public float challenge;
        public float speed, launchTime;
        public Rect opening;
        public DodgePose pose;
        public List<BeamSegment> beams;
    }

    public sealed class SafetyCertificate
    {
        public List<Vector2> route = new List<Vector2>();
        public List<WavePlan> waves = new List<WavePlan>();
        public float targetX, playerZ, leadTime, pathLength;
        public float reservationUntil;
    }
}
