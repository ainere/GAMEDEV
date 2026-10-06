using System.Collections.Generic;
using UnityEngine;
namespace LaserCorridor
{
    public static class TileGrid
    {
        public static Vector2 Center(int column,int row,RunSettings s) => new Vector2((column-2)*s.tilePitch,(row+.5f)*s.tilePitch);
        public static bool Eligible(int column,int row) => row>=2&&row<=13;
        public static List<Vector2> EligibleCenters(RunSettings s)
        {var result=new List<Vector2>();for(int row=2;row<=13;row++)for(int col=0;col<5;col++)if(Eligible(col,row))result.Add(Center(col,row,s));return result;}
    }
}
