using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Teste.Models;

public class Pedido
{
    [Key]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Nome do cliente é obrigatório")]
    [MaxLength(100)]
    [Display(Name = "Nome do Cliente")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório")]
    [MaxLength(200)]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [Display(Name = "Email do Cliente")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Display(Name = "Data do Pedido")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pendente";

    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Valor Total")]
    public decimal TotalAmount { get; set; }

    public List<ItemPedido> Itens { get; set; } = [];
}
