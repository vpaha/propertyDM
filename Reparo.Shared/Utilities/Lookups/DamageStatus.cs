public enum DamageStatus
{
    Reported = 1,
    WaitingForVendorAssignment = 2,
    VendorAssigned = 3,

    InspectionScheduled = 4,
    InspectionCompleted = 5,
    EstimatePending = 6,
    
    ServiceScheduled = 7,
    WorkInProgress = 8,

    WorkCompleted = 9,
    Closed = 10,
    Cancelled = 11,
    OnHold = 12
}