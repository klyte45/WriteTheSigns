using UnityEngine;

namespace Klyte.WriteTheSigns.Rendering
{
    internal static class WTSDisplayContainerMeshUtils
    {
        public const float FRONT_HEIGHT_BASE = 1f;
        public const float BACK_HEIGHT_BASE = 1f;
        public const float FRONT_WIDTH_BASE = 1f;
        public const float BACK_WIDTH_BASE = 1f;
        public const float DEPTH_BASE = 0.5f;
        public const float FRONT_BORDER_BASE = 0.05f;


        public static void GenerateDisplayContainer(Vector2 frontWH, Vector2 backWH, Vector2 backCenterOffset, float depth, float frontBorderThickness, out Vector3[] points)
        {
            /**
             * 
             *  P0.________________________________________________.P1
             *    |  I0________________________________________I1  |
             *    |    |                                      |    |
             *    |    |                                      |    |
             *    |    |                                      |    |
             *    |    |                                      |    |
             *    |    |                                      |    |
             *    |  I2|______________________________________|I3  |
             *    |________________________________________________|
             *  P2                                                  P3
             *  
             *       /|P0
             *      / |
             *     /  |     
             *  Q0/   |    Q1.____________________________________________.Q0
             *    |   |      |                                            |
             *    |   |      |                                            |
             *    |   |      |                                            |       
             *    |___|      |____________________________________________|
             *  Q2     P2  Q3                                             Q2
             *  
             *  P0-P1 = frontWH.x
             *  P0-P2 = frontWH.y 
             *  Q0-Q1 = backWH.x
             *  Q0-Q2 = backWH.y  
             *  
             *  (P0-P1)/2 - (Q0-Q1)/2 = backCenterOffset.x
             *  (P0-P2)/2 - (Q0-Q2)/2 = backCenterOffset.y
             *  
             *  (P0_P2)|D|(Q0_Q2) = depth
             *  (I0-P0)² = frontBorderThickness  
             *  
             *  point | index
             *  =============
             *  P0    |   0
             *  P1    |   1
             *  P2    |   2
             *  P3    |   3
             *  I0    |   4
             *  I1    |   5
             *  I2    |   6
             *  I3    |   7
             *  Q0    |   8
             *  Q1    |   9     
             *  Q2    |  10
             *  Q3    |  11 
             */


            points = new Vector3[]
            {
                new Vector3(-frontWH.x/2, frontWH.y,depth/2), //P0
                new Vector3(frontWH.x/2, frontWH.y,depth/2),  //P1
                new Vector3(-frontWH.x/2, 0,depth/2),//P2
                new Vector3(frontWH.x/2, 0,depth/2), //P3
                new Vector3(-frontWH.x/2+frontBorderThickness, frontWH.y-frontBorderThickness,depth/2), //I0
                new Vector3(frontWH.x/2-frontBorderThickness, frontWH.y-frontBorderThickness,depth/2),  //I1
                new Vector3(-frontWH.x/2+frontBorderThickness,frontBorderThickness,depth/2),//I2
                new Vector3(frontWH.x/2-frontBorderThickness, frontBorderThickness,depth/2), //I3
                new Vector3(-backWH.x/2, backWH.y,-depth/2)+ (Vector3) backCenterOffset, //Q0
                new Vector3(backWH.x/2, backWH.y,-depth/2)+ (Vector3) backCenterOffset,  //Q1
                new Vector3(-backWH.x/2, 0,-depth/2)+ (Vector3) backCenterOffset,//Q2
                new Vector3(backWH.x/2,0,-depth/2)+ (Vector3) backCenterOffset, //Q3
            };
        }

        public static readonly int[] m_triangles = new int[]
        {
            // FRONT
            0,4,1,    // P0-I0-P1
            4,5,1,    // I0-I1-P1
            1,5,3,    // P1-I1-P3
            5,7,3,    // I1-I3-P3
            7,6,3,    // I3-I2-P3
            6,2,3,    // I2-P2-P3
            4,2,6,    // I0-P2-I2
            0,2,4,    // P0-P2-I0
            
            //LEFT
            0,8,2,    // P0-Q0-P2
            8,10,2,   // Q0-Q2-P2

            //RIGHT
            3,9,1,    // P3-Q1-P1
            3,11,9,   // P3-Q3-Q1

            //TOP
            1,8,0,    // P1-Q0-P0
            1,9,8,    // P1-Q1-Q0
            
            //BOTTOM
            10,3,2,   // P3-Q2-P2
            11,3,10,  // P2-Q3-Q2

            //BACK
            8,9,10,   // Q0-Q1-Q2
            9,11,10,  // Q1-Q3-Q2


        };

        public static readonly int[] m_trianglesGlass = new int[]
        {
            4,6,5,    // I0-I1-I3
            7,5,6,    // I3-I1-I2
        };
    }


}
