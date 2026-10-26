namespace PizzeriaPadua.API.Services
{
    public class UsernameValidationService
    {
        private static readonly HashSet<string> ForbiddenWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "puto", "puta", "pendejo", "pendeja", "pinche", "mierda", "verga", "culo",
            "pito", "chinga", "cabron", "cabrón", "coño", "joto", "maricon", "maricón",
            "naco", "naca", "wey", "güey", "mamón", "mamado", "toto",
            "sex", "porno", "nude", "fuck", "shit", "bitch", "asshole", "dick", "pussy",
            "admin", "administrador", "root", "system", "moderador", "moderator",
            "soporte", "support", "ayuda", "help", "info", "informacion"
        };

        public (bool isValid, string message) ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "El nombre de usuario no puede estar vacío");

            var trimmed = username.Trim();
            if (trimmed.Length < 3)
                return (false, "El nombre de usuario debe tener al menos 3 caracteres");
            if (trimmed.Length > 50)
                return (false, "El nombre de usuario no puede tener más de 50 caracteres");

            foreach (var word in ForbiddenWords)
            {
                if (trimmed.ToLower().Contains(word.ToLower()))
                    return (false, "Este nombre de usuario contiene palabras no permitidas");
            }

            return (true, "");
        }

        public (bool isValid, string message) ValidateText(string text, string fieldName = "texto")
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, $"El {fieldName} no puede estar vacío");

            foreach (var word in ForbiddenWords)
            {
                if (text.ToLower().Contains(word.ToLower()))
                    return (false, $"El {fieldName} contiene palabras no permitidas");
            }

            return (true, "");
        }
    }
}
