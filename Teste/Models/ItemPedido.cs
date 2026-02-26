using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Teste.Models;

public class ItemPedido
{
    [Key]
    public int ItemId { get; set; }

    [Required]
    [Display(Name = "Pedido")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "ID do produto é obrigatório")]
    [Display(Name = "ID do Produto")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Quantidade é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "Mínimo 1")]
    [Display(Name = "Quantidade")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Preço unitário é obrigatório")]
    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Preço Unitário")]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    [Display(Name = "Subtotal")]
    public decimal LineTotal => Quantity * UnitPrice;

    [ForeignKey(nameof(OrderId))]
    public Pedido? Pedido { get; set; }
}
