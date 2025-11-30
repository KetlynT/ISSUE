namespace GraficaModerna.Domain.Entities
{
    // CORRIGIDO: de internal class para public class
    public class ContentPage
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public ContentPage() { } // Construtor vazio para EF

        public ContentPage(string slug, string title, string content)
        {
            Id = Guid.NewGuid();
            Slug = slug;
            Title = title;
            Content = content;
        }
    }
}