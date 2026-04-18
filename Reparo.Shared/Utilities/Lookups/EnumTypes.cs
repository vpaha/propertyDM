
public enum ScreenSize
{
    Mobile,
    Tablet,
    Laptop,
    Large
}
public enum ActionType
{
    Submit,
    Save,
    Cancel,
    SubmitToStep,
    Route
}
public enum AddressType
{
    Physical,
    Mailing
}
public enum PhoneType
{
    Primary,
    Secondary,
    Mobile,
    Fax,
    Emergency,
    Pager,
    SecondaryFax,
    SecureFax
}

public enum ProviderStatus
{
    Active,
    Inactive,
    Incomplete
}

public enum ProviderCredentialStatus
{
    NotRequired,
    Provisional,
    Credentialed,
    Uncredentialed
}

public enum UdfTypeList
{
    DATE,
    FREETEXT,
    NUMERIC,
    LABELONLY,
    SELECTABLE
}

public enum MessageSeverity
{
    Information,
    Warning,
    Error,
    ServerException
}

public enum MessageCategory
{
    Global,
    Module,
    Actions
}

public enum MessageSource
{
    UI,
    API,
    External
}

public enum IncidentWFStatus
{
    Locked,
    SetAside,
    Resume,
    NoStatus,
    TransIdNullWithoutResume,
    TransIdNullWithResume,
    TransIdNotNull,
    Suspended
}