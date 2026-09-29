using System;
using System.Collections.Generic;

namespace LabStock.Models
{
    public enum UserRole
    {
        Admin,          // Администратор (Полный доступ, пользователи, настройки, backup)
        HeadOfLab,      // Заведующий (Склад, анализы, отчёты, поступления, списания)
        LabAssistant,   // Лаборант (Выполнение анализов, просмотр, списания)
        Viewer          // Просмотр (Dashboard, склад, отчёты без изменения)
    }

    public static class UserRoleExtensions
    {
        public static string ToFriendlyName(this UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "Администратор",
                UserRole.HeadOfLab => "Заведующий лабораторией",
                UserRole.LabAssistant => "Лаборант",
                UserRole.Viewer => "Только просмотр",
                _ => role.ToString()
            };
        }
    }

    public enum MovementType
    {
        Receipt,            // Поступление
        AnalysisRun,        // Списание по анализу
        ManualWriteoff,     // Ручное списание
        InventoryAdjustment,// Корректировка инвентаризации
        InitialBalance      // Начальный остаток
    }

    public static class MovementTypeExtensions
    {
        public static string ToFriendlyName(this MovementType type)
        {
            return type switch
            {
                MovementType.Receipt => "Поступление",
                MovementType.AnalysisRun => "Анализ",
                MovementType.ManualWriteoff => "Ручное списание",
                MovementType.InventoryAdjustment => "Корректировка",
                MovementType.InitialBalance => "Начальный остаток",
                _ => type.ToString()
            };
        }
    }

    public enum StockStatus
    {
        Normal,         // Выше минимума (Норма)
        Attention,      // Близко к минимуму (Внимание) <= MinQuantity * 1.2
        Low,            // Ниже минимума (Низкий остаток) <= MinQuantity
        OutOfStock      // 0 (Нет на складе)
    }

    public enum NotificationType
    {
        LowStock,
        CriticalStock,
        ExpiringSoon,
        Expired,
        OutOfStock,
        Info
    }
}
