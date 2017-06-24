namespace DiscordMafia.Roles
{
    public class RobinHood : NeutralKiller
    {
        public override string Name
        {
            get
            {
                return "Робин Гуд";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "Робин Гуд",
                    "Робин Гуда",
                    "Робин Гуду",
                    "Робин Гуда",
                    "Робин Гудом",
                    "Робин Гуде",
                };
            }
        }
    }
}
