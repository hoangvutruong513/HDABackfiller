using System.Collections.Generic;

namespace Core.FileReader
{
    public interface IReader
    {
        IList<string> readFile();

    }
}
