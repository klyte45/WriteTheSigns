using Klyte.Commons.Interfaces;
using Klyte.DynamicTextProps.Utils;
using static Klyte.DynamicTextProps.TextureAtlas.LineIconsTextureAtlas;

namespace Klyte.DynamicTextProps.TextureAtlas
{
    public class LineIconsTextureAtlas : TextureAtlasDescriptor<LineIconsTextureAtlas, DTPResourceLoader, LineIcon>
    {
        protected override string ResourceName => "UI.Images.lineFormats.png";

        protected override string CommonName => "LineIcons";

        public enum LineIcon
        {
            MapIcon,
            OvalIcon,
            RoundedHexagonIcon,
            RoundedPentagonIcon,
            RoundedTriangleIcon,
            OctagonIcon,
            HeptagonIcon,
            S10StarIcon,
            S09StarIcon,
            S07StarIcon,
            S06StarIcon,
            S05StarIcon,
            S04StarIcon,
            S03StarIcon,
            CameraIcon,
            MountainIcon,
            ConeIcon,
            TriangleIcon,
            CrossIcon,
            DepotIcon,
            ParachuteIcon,
            HexagonIcon,
            SquareIcon,
            PentagonIcon,
            TrapezeIcon,
            DiamondIcon,
            S08StarIcon,         
            CircleIcon,
            RoundedSquareIcon
        };
    }
}
