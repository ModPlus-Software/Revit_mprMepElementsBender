namespace mprMepElementsBender.Helpers.Enums
{
    /// <summary>
    /// Вариант работы при возникновении ошибки
    /// </summary>
    public enum OnExceptionVariant
    {
        /// <summary>
        /// Принять и продолжить
        /// </summary>
        AcceptAndContinue,

        /// <summary>
        /// Отменить и продолжить
        /// </summary>
        RejectAndContinue,

        /// <summary>
        /// Прервать все
        /// </summary>
        AbortAll
    }
}
