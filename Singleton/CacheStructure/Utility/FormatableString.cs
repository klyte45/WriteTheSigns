using Klyte.WriteTheSigns.Utils;

namespace Klyte.WriteTheSigns.Rendering
{
    public class FormatableString
    {
        private string value;
        private string caps;
        private string abbreviated;
        private string capsAbbreviated;
        
        public FormatableString(string value) => Value = value;

        public string Get(bool uppercase, bool abbreviated)
            => uppercase
                ? abbreviated ? CapsAbbreviated : Caps
                : abbreviated ? Abbreviated : Value;

        public string Value
        {
            get => value; set
            {
                this.value = value ?? "";
                caps = null;
                abbreviated = null;
                capsAbbreviated = null;
            }
        }
        public string Caps
        {
            get
            {
                if (caps is null)
                {
                    caps = value.ToUpper();
                }
                return caps;
            }
        }
        public string Abbreviated
        {
            get
            {
                if (abbreviated is null)
                {
                    abbreviated = WTSUtils.ApplyAbbreviations(value);
                }
                return abbreviated;
            }
        }
        public string CapsAbbreviated
        {
            get
            {
                if (capsAbbreviated is null)
                {
                    capsAbbreviated = Abbreviated.ToUpper();
                }
                return capsAbbreviated;
            }
        }
    }

}
