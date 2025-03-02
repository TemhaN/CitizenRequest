using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitizenRequest.Models
{
    public class CitizenRegisterViewModel
    {
        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "ФИО")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Phone(ErrorMessage = "Некорректный номер телефона")]
        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Адрес")]
        public string Address { get; set; }
    }

    public class ResponseViewModel
    {
        [Required(ErrorMessage = "Введите сообщение")]
        [Display(Name = "Сообщение")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "Сообщение должно содержать от 5 до 1000 символов")]
        public string Message { get; set; }
    }

    public class DashboardViewModel
    {
        public Citizen Citizen { get; set; }
        public IEnumerable<CitizenApplication> Applications { get; set; } // Исправлено на CitizenApplication
    }

    public class ApplicationCreateViewModel
    {
        [HiddenInput]
        [Required(ErrorMessage = "Выберите категорию")]
        [Display(Name = "Категория обращения")]
        public int SelectedCategoryId { get; set; }

        [Display(Name = "Категория")]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Опишите проблему")]
        [Display(Name = "Описание проблемы")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Описание должно содержать от 20 до 2000 символов")]
        public string Description { get; set; }

        [BindNever]
        [ValidateNever] // Добавьте этот атрибут, чтобы исключить поле из валидации
        public IEnumerable<SelectListItem> Categories { get; set; }
    }


    public class ApplicationDetailsViewModel
    {
        public CitizenApplication Application { get; set; } // Исправлено на CitizenApplication
        public ResponseViewModel NewMessage { get; set; }
    }

    public class Citizen
    {
        public int CitizenId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<CitizenSession> Sessions { get; set; }
    }

    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }

    public enum ApplicationStatus
    {
        [Display(Name = "Новое")]
        New,
        [Display(Name = "В обработке")]
        InProgress,
        [Display(Name = "Рассмотрено")]
        Resolved,
        [Display(Name = "Отклонено")]
        Rejected
    }

    public class Response
    {
        public int ResponseId { get; set; }
        public int ApplicationId { get; set; } // Исправлено на CitizenApplication
        public CitizenApplication Application { get; set; } // Исправлено на CitizenApplication
        public int? AdminId { get; set; }
        public Admin Admin { get; set; }
        public string ResponseText { get; set; }
        public DateTime ResponseDate { get; set; }
        public bool IsFromCitizen { get; set; }
    }

    public class CitizenSession
    {
        [Key] // Указываем первичный ключ
        public int SessionId { get; set; }

        public int CitizenId { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }

        public Citizen Citizen { get; set; }

        public string AuthenticationScheme { get; set; } = "CitizenCookie";
    }

    [Table("Applications")]
    public class CitizenApplication // Переименовано с Application
    {
        [Key]
        [Column("ApplicationId")]
        public int ApplicationId { get; set; }
        public int CitizenId { get; set; }
        public Citizen Citizen { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime? ResolutionDate { get; set; }
        public string Description { get; set; }
        public virtual ICollection<Response> Responses { get; set; } // Добавьте эту строку
    }

    public class Admin
    {
        public int AdminId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }


    public class HomePageViewModel
    {
        [BindNever]
        [ValidateNever]
        public IEnumerable<Category> Categories { get; set; }
        public CitizenRegisterViewModel RegisterModel { get; set; }
        [BindNever]
        [ValidateNever]
        public int? SelectedCategoryId { get; set; }
    }

}