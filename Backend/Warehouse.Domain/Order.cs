namespace Warehouse.Domain;

public enum OrderStatus
{
    New,        // Упал в систему, еще никто не трогал
    Picking,    // Кладовщик собирает его прямо сейчас
    Packed,     // Собран, лежит на рампе
    Shipped,    // Уехал к клиенту
    Canceled    // Отменен
}

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}