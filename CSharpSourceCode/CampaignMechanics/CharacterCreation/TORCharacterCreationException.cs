using System;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    public class TORCharacterCreationException : Exception
    {
        public TORCharacterCreationException(string message) : base(message)
        {
        }

        public TORCharacterCreationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class TORCCXmlLoadException : TORCharacterCreationException
    {
        public string FilePath { get; }

        public TORCCXmlLoadException(string filePath, Exception innerException)
            : base($"Failed to load character creation XML from '{filePath}'", innerException)
        {
            FilePath = filePath;
        }
    }

    public class TORCCSpecializationStageLoadException : TORCharacterCreationException
    {
        public TORCCSpecializationStageLoadException(string message, Exception innerException)
            : base($"Failed to initialize specialization stage: {message}", innerException)
        {
        }
    }

    public class TORCCReflectionException : TORCharacterCreationException
    {
        public string FieldName { get; }

        public TORCCReflectionException(string fieldName, Exception innerException)
            : base($"Failed to access CharacterCreationManager field '{fieldName}'", innerException)
        {
            FieldName = fieldName;
        }
    }

    public class TORCCEquipmentUpdateException(string equipmentSetId, Exception innerException)
        : TORCharacterCreationException($"Failed to update equipment with roster '{equipmentSetId}'", innerException)
    {
        public string EquipmentSetId { get; } = equipmentSetId;
    }

    public class TORCCInvalidOptionTypeException : TORCharacterCreationException
    {
        public Type ExpectedType { get; }
        public Type ActualType { get; }

        public TORCCInvalidOptionTypeException(Type expectedType, Type actualType)
            : base($"Expected option data type '{expectedType.Name}', but received '{actualType?.Name ?? "null"}'")
        {
            ExpectedType = expectedType;
            ActualType = actualType;
        }
    }
}
