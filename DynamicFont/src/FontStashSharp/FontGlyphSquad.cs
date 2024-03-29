﻿using System.Runtime.InteropServices;

namespace FontStashSharp
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct FontGlyphSquad
	{
		public float X0;
		public float Y0;
		public float S0;
		public float T0;
		public float X1;
		public float Y1;
		public float S1;
		public float T1;

        public override string ToString() => $"[x[{X0}-{X1}];y[{Y0}-{Y1}];s[{S0}-{S1}];t[{T0}-{T1}]]";
    }
}
