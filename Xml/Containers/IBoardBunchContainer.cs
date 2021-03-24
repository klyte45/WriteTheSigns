namespace Klyte.WriteTheSigns.Xml
{
    public interface IBoardBunchContainer
    {
        bool HasAnyBoard();
    }

    public class BasicBoardBunchContainer : IBoardBunchContainer
    {
        public bool HasAnyBoard() => false;
    }


}
