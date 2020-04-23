namespace Klyte.WriteTheSigns.Xml
{
    public interface IBoardBunchContainer
    {
        public bool HasAnyBoard();
    }

    public class BasicBoardBunchContainer : IBoardBunchContainer
    {
        public bool HasAnyBoard() => false;
    }


}
