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


        public static void GenerateDisplayContainer(Vector2 frontWH, Vector2 backWH, Vector2 backCenterOffset, float frontDepth, float backDepth, float frontBorderThickness, out Vector3[] points, out Vector4[] tangents)
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
             *         As0._____ 
             *          _/     |Ps0
             *        _/       |
             *      _/         |     
             *  Qs0/           |    Q1.____________________________________________.Q0
             *     |           |      |                                            |
             *     |           |      |                                            |       
             *     |           |      |                                            |
             *     |___________|      |____________________________________________|
             *  Qs2    As2     Ps2  Q3                                             Q2
             *  
             *  P0-P1 = A0-A1 = frontWH.x
             *  P0-P2 = A0-A2 = frontWH.y 
             *  Q0-Q1 = backWH.x
             *  Q0-Q2 = backWH.y  
             *  
             *  (A0-A1)/2 - (Q0-Q1)/2 = backCenterOffset.x
             *  (A0-A2)/2 - (Q0-Q2)/2 = backCenterOffset.y
             *  
             *  (P0_P2) - (A0_A2) = frontDepth
             *  (Q0_Q2) - (A0_A2) = backDepth
             *  (I0-P0)² = frontBorderThickness  
             *  
             *  point | index
             *  =============
             *  I0    |   0
             *  I1    |   1
             *  I2    |   2
             *  I3    |   3
             *  P0    |   4
             *  P1    |   5
             *  P2    |   6
             *  P3    |   7             
             *  
             *  Q0    |   8
             *  Q1    |   9     
             *  Q2    |  10
             *  Q3    |  11 
             *  
             *  As0   |  12
             *  As1   |  13     
             *  As2   |  14
             *  As3   |  15  
             *  Ps0   |  16
             *  Ps1   |  17
             *  Ps2   |  18
             *  Ps3   |  19
             *  Qs0   |  20
             *  Qs1   |  21     
             *  Qs2   |  22
             *  Qs3   |  23                
             *  
             *  Az0   |  24
             *  Az1   |  25  
             *  Az2   |  26
             *  Az3   |  27 
             *  Pz0   |  28
             *  Pz1   |  29
             *  Pz2   |  30
             *  Pz3   |  31
             *  Qz0   |  32
             *  Qz1   |  33    
             *  Qz2   |  34
             *  Qz3   |  35 
             */

            var I0 = new Vector3(-frontWH.x / 2 + frontBorderThickness, frontWH.y - frontBorderThickness, frontDepth); //I0
            var I1 = new Vector3(frontWH.x / 2 - frontBorderThickness, frontWH.y - frontBorderThickness, frontDepth);  //I1
            var I2 = new Vector3(-frontWH.x / 2 + frontBorderThickness, frontBorderThickness, frontDepth);//I2
            var I3 = new Vector3(frontWH.x / 2 - frontBorderThickness, frontBorderThickness, frontDepth); //I3                
            var P0 = new Vector3(-frontWH.x / 2, frontWH.y, frontDepth); //P0
            var P1 = new Vector3(frontWH.x / 2, frontWH.y, frontDepth);  //P1
            var P2 = new Vector3(-frontWH.x / 2, 0, frontDepth);//P2
            var P3 = new Vector3(frontWH.x / 2, 0, frontDepth); //P3
            var Q0 = new Vector3(-backWH.x / 2, backWH.y, -backDepth) + (Vector3)backCenterOffset; //Q0
            var Q1 = new Vector3(backWH.x / 2, backWH.y, -backDepth) + (Vector3)backCenterOffset; //Q1
            var Q2 = new Vector3(-backWH.x / 2, 0, -backDepth) + (Vector3)backCenterOffset;//Q2
            var Q3 = new Vector3(backWH.x / 2, 0, -backDepth) + (Vector3)backCenterOffset; //Q3
            var A0 = new Vector3(-frontWH.x / 2, frontWH.y, 0); //A0
            var A1 = new Vector3(frontWH.x / 2, frontWH.y, 0);  //A1
            var A2 = new Vector3(-frontWH.x / 2, 0, 0);//A2
            var A3 = new Vector3(frontWH.x / 2, 0, 0); //A3

            points = new Vector3[]
            {
               I0 ,
               I1 ,
               I2 ,
               I3 ,
               P0 ,
               P1 ,
               P2 ,
               P3 ,
                  
               Q0 ,
               Q1 ,
               Q2 ,
               Q3 ,
                  
               A0 ,
               A1 ,
               A2 ,
               A3 ,
               P0 ,
               P1 ,
               P2 ,
               P3 ,
               Q0 ,
               Q1 ,
               Q2 ,
               Q3 ,
                  
               A0 ,
               A1 ,
               A2 ,
               A3 ,
               P0 ,
               P1 ,
               P2 ,
               P3 ,
               Q0 ,
               Q1 ,
               Q2 ,
               Q3
            };

            tangents = new Vector4[]
            {
           /* 0 */   new Vector4(1,0,0,1),
           /* 1 */   new Vector4(1,0,0,1),
           /* 2 */   new Vector4(1,0,0,1),
           /* 3 */   new Vector4(1,0,0,1),
           /* 4 */   new Vector4(1,0,0,1),
           /* 5 */   new Vector4(1,0,0,1),
           /* 6 */   new Vector4(1,0,0,1),
           /* 7 */   new Vector4(1,0,0,1),
           /*   */
           /* 8 */   new Vector4(1,0,0,-1),
           /* 9 */   new Vector4(1,0,0,-1),
           /*10 */   new Vector4(1,0,0,-1),
           /*11 */   new Vector4(1,0,0,-1),
           /*   */
           /*12 */   new Vector4(0,0,1,-1),
           /*13 */   new Vector4(0,0,1,1),   
           /*14 */   new Vector4(0,0,1,-1),   
           /*15 */   new Vector4(0,0,1,1),   
           /*16 */   new Vector4(0,0,1,-1),   
           /*17 */   new Vector4(0,0,1,1),   
           /*18 */   new Vector4(0,0,1,-1),   
           /*19 */   new Vector4(0,0,1,1),   
           /*20 */   new Vector4(0,0,1,-1),   
           /*21 */   new Vector4(0,0,1,1),   
           /*22 */   new Vector4(0,0,1,-1),   
           /*23 */   new Vector4(0,0,1,1),   
           /*   */   
           /*24 */   new Vector4(0,1,0,1),
           /*25 */   new Vector4(0,1,0,1),
           /*26 */   new Vector4(0,1,0,-1),   
           /*27 */   new Vector4(0,1,0,-1),   
           /*28 */   new Vector4(0,1,0,1),   
           /*29 */   new Vector4(0,1,0,1),   
           /*30 */   new Vector4(0,1,0,-1),   
           /*31 */   new Vector4(0,1,0,-1),   
           /*32 */   new Vector4(0,1,0,1),   
           /*33 */   new Vector4(0,1,0,1),   
           /*34 */   new Vector4(0,1,0,-1),   
           /*35 */   new Vector4(0,1,0,-1),   
            };
        }

        public static readonly int[] m_triangles = new int[]
        {
           // FRONT
           4,0,5,    // P0-I0-P1
           0,1,5,    // I0-I1-P1
           5,1,7,    // P1-I1-P3
           1,3,7,    // I1-I3-P3
           3,2,7,    // I3-I2-P3
           2,6,7,    // I2-P2-P3
           0,6,2,    // I0-P2-I2
           4,6,0,    // P0-P2-I0
            
           //BACK     
           8,9,10,   // Q0-Q1-Q2
           9,11,10,  // Q1-Q3-Q2
            
           //LEFT     
           12,20,14,  // As0-Qs0-As2
           20,22,14,  // Qs0-Qs2-As2
           16,12,18,  // Ps0-As0-Ps2
           12,14,18,  // As0-As2-Ps2
                
           //RIGHT    
           15,21,13,  // As3-Qs1-As1
           15,23,21,  // As3-Qs3-Qs1
           19,13,17,  // Ps3-As1-Ps1
           19,15,13,  // Ps3-As3-As1
            
           //TOP      
           25,32,24,  // Az1-Qz0-Az0
           25,33,32,  // Az1-Qz1-Qz0
           29,24,28,  // Pz1-Az0-Pz0
           29,25,24,  // Pz1-Az1-Az0
            
           //BOTTOM   
           34,27,26,  // Qz2-Az3-Az2
           35,27,34,  // Qz3-Az3-Qz2
           26,31,30,  // Az2-Pz3-Pz2
           27,31,26,  // Az3-Pz3-Az2



        };

        public static readonly int[] m_trianglesGlass = new int[]
        {
             0,2,1,    // I0-I2-I1
             3,1,2,    // I3-I1-I2
        };
    }


}




































