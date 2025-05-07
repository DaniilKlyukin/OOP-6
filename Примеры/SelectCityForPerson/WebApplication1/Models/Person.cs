namespace WebApplication1.Models;

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CityId { get; set; }  // Внешний ключ
    public City City { get; set; }   // Навигационное свойство
}
