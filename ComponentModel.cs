using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace System.ComponentModel.DataAnnotations
{
    public class RequiredAttribute : Attribute
    {
        public string ErrorMessage { get; set; }
    }

    public class DisplayAttribute : Attribute
    {
        public string Name { get; set; }
    }

    public sealed class ValidationResult
    {
        // Summary:
        //     A value that indicates the entity member successfully validated.
        public static readonly ValidationResult Success;

        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationResult
        //     class with the specified error message.
        //
        // Parameters:
        //   errorMessage:
        //     The error message to display to the user. If null, the System.ComponentModel.DataAnnotations.ValidationAttribute.GetValidationResult(System.Object,System.ComponentModel.DataAnnotations.ValidationContext)
        //     method uses the System.ComponentModel.DataAnnotations.ValidationAttribute.FormatErrorMessage(System.String)
        //     method to create the error message.
        public ValidationResult(string errorMessage) { this.ErrorMessage = errorMessage; }
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationResult
        //     class with the specified error message and a collection of member names that
        //     are associated with the validation result.
        //
        // Parameters:
        //   errorMessage:
        //     The error message to display to the user. If null, the System.ComponentModel.DataAnnotations.ValidationAttribute.GetValidationResult(System.Object,System.ComponentModel.DataAnnotations.ValidationContext)
        //     method uses the System.ComponentModel.DataAnnotations.ValidationAttribute.FormatErrorMessage(System.String)
        //     method to create the error message.
        //
        //   memberNames:
        //     The collection of member names associated with the validation result. If
        //     empty, System.ComponentModel.DataAnnotations.ValidationAttribute.GetValidationResult(System.Object,System.ComponentModel.DataAnnotations.ValidationContext)
        //     will construct this list from the System.ComponentModel.DataAnnotations.ValidationContext.MemberName
        //     property.
        public ValidationResult(string errorMessage, IEnumerable<string> memberNames) { this.ErrorMessage = errorMessage; this.MemberNames = memberNames; }

        // Summary:
        //     Gets or sets the error message for the validation result.
        //
        // Returns:
        //     The error message for the validation result.
        public string ErrorMessage { get; set; }
        //
        // Summary:
        //     Gets the collection of member names associated with the validation result.
        //
        // Returns:
        //     The collection of member names associated with the validation result.
        public IEnumerable<string> MemberNames { get; set; }

        // Summary:
        //     Returns a string value that represents the current validation result.
        //
        // Returns:
        //     A string value that represents the current validation result.
        public override string ToString() { return string.Empty; }
    }

    // Summary:
    //     Provides information about a type or member to validate.
    public sealed class ValidationContext : IServiceProvider
    {
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationContext
        //     class with the specified object to validate, a service provider that enables
        //     validation methods to access external services, and a collection of values
        //     related to validation.
        //
        // Parameters:
        //   instance:
        //     The object to validate.
        //
        //   serviceProvider:
        //     A service provider that enables validation methods to access external services.
        //     This value can be null.
        //
        //   items:
        //     A collection of values related to validation. This value can be null.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     instance is null.
        public ValidationContext(object instance, IServiceProvider serviceProvider, IDictionary<object, object> items)
        {
            this.objectInstance = instance;
            this.serviceProvider = serviceProvider;
            this.items = items;
        }

        // Summary:
        //     Gets or sets the name to display to users for the member to validate.
        //
        // Returns:
        //     The name of member to validate.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     value is null or an empty string ("").
        public string DisplayName { get; set; }
        //
        // Summary:
        //     Gets the collection of values associated with the validation request.
        //
        // Returns:
        //     The collection of values associated with the validation request
        public IDictionary<object, object> Items { get { return items; } }
        private IDictionary<object, object> items;
        //
        // Summary:
        //     Gets or sets the programmatic name of the member to validate.
        //
        // Returns:
        //     The programmatic name of the member to validate.
        public string MemberName { get; set; }
        //
        // Summary:
        //     Gets the object to validate.
        //
        // Returns:
        //     The object to validate.
        public object ObjectInstance { get { return objectInstance; } }
        private object objectInstance;
        //
        // Summary:
        //     Gets the type of the object to validate.
        //
        // Returns:
        //     The type of the object to validate.
        public Type ObjectType { get { return objectInstance.GetType(); } }

        // Summary:
        //     Retrieves an instance of the service to use during validation.
        //
        // Parameters:
        //   serviceType:
        //     The type of the service to use during validation.
        //
        // Returns:
        //     An instance of that service or null if the service is not available.
        public object GetService(Type serviceType)
        {
            if (serviceProvider != null && serviceType.IsAssignableFrom(serviceProvider.GetType()))
                return serviceProvider;
            else
                return null;
        }

        private IServiceProvider serviceProvider;
    }

    // Summary:
    //     Serves as the base class for all validation attributes.
    public abstract class ValidationAttribute : Attribute
    {
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationAttribute
        //     class.
        protected ValidationAttribute() { }
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationAttribute
        //     class with the specified function to retrieve the error message.
        //
        // Parameters:
        //   errorMessageAccessor:
        //     A function to retrieve the error message.
        protected ValidationAttribute(Func<string> errorMessageAccessor) { this.errorMessageAccessor = errorMessageAccessor; }
        private Func<string> errorMessageAccessor = null;
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationAttribute
        //     class with the specified error message.
        //
        // Parameters:
        //   errorMessage:
        //     A non-localizable error message.
        protected ValidationAttribute(string errorMessage) { this.errorMessage = errorMessage; }
        private string errorMessage = null;

        // Summary:
        //     Gets or sets the non-localizable error message to display when validation
        //     fails.
        //
        // Returns:
        //     The non-localizable error message.
        public string ErrorMessage
        {
            get
            {
                if (this.errorMessageAccessor != null)
                    return errorMessageAccessor();
                else
                    return errorMessage;
            }

            set { errorMessage = value; }
        }
        //
        // Summary:
        //     Gets or sets the property name on the resource type that provides the localizable
        //     error message.
        //
        // Returns:
        //     The property name on the resource type that provides the localizable error
        //     message.
        public string ErrorMessageResourceName { get; set; }
        //
        // Summary:
        //     Gets or sets the resource type that provides the localizable error message.
        //
        // Returns:
        //     The resource type that provides the localizable error message.
        public Type ErrorMessageResourceType { get; set; }
        //
        // Summary:
        //     Gets the localized or non-localized error message.
        //
        // Returns:
        //     The localized or non-localized error message.
        //
        // Exceptions:
        //   System.InvalidOperationException:
        //     Both System.ComponentModel.DataAnnotations.ValidationAttribute.ErrorMessageResourceName
        //     and System.ComponentModel.DataAnnotations.ValidationAttribute.ErrorMessage
        //     are set.-or-Neither System.ComponentModel.DataAnnotations.ValidationAttribute.ErrorMessageResourceName
        //     nor System.ComponentModel.DataAnnotations.ValidationAttribute.ErrorMessage
        //     is set.-or-Either System.ComponentModel.DataAnnotations.ValidationAttribute.ErrorMessageResourceName
        //     or System.ComponentModel.DataAnnotations.ValidationAttribute.ErrorMessageResourceType
        //     is set, but the other is not.
        protected string ErrorMessageString { get { return errorMessage; } }

        // Summary:
        //     Applies formatting to the error message.
        //
        // Parameters:
        //   name:
        //     The name to include in the formatted error message.
        //
        // Returns:
        //     The formatted error message.
        public virtual string FormatErrorMessage(string name) { return name; }
        //
        // Summary:
        //     Determines whether the specified object is valid and returns an object that
        //     includes the results of the validation check.
        //
        // Parameters:
        //   value:
        //     The object to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        // Returns:
        //     System.ComponentModel.DataAnnotations.ValidationResult.Success if the value
        //     is valid; otherwise, an instance of the System.ComponentModel.DataAnnotations.ValidationResult
        //     class with the error message.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     validationContext is null.
        public ValidationResult GetValidationResult(object value, ValidationContext validationContext)
        {
            if (validationContext == null)
                throw new ArgumentNullException("validationContext");
            return IsValid(value, validationContext);
        }
        //
        // Summary:
        //     Determines whether the specified object is valid.
        //
        // Parameters:
        //   value:
        //     The object to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        // Returns:
        //     true if value is valid; otherwise, false.
        //
        // Exceptions:
        //   System.NotImplementedException:
        //     The System.ComponentModel.DataAnnotations.ValidationAttribute.IsValid(System.Object,System.ComponentModel.DataAnnotations.ValidationContext)
        //     method is not overridden in the derived class.
        protected virtual ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Determines whether the specified object is valid and throws a System.ComponentModel.DataAnnotations.ValidationException
        //     if the object is not valid.
        //
        // Parameters:
        //   value:
        //     The object to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     validationContext is null.
        //
        //   System.ComponentModel.DataAnnotations.ValidationException:
        //     value is not valid.
        public void Validate(object value, ValidationContext validationContext)
        {
            if (validationContext == null)
                throw new ArgumentNullException("validationContext");
            ValidationResult vr = IsValid(value, validationContext);
            if (vr != ValidationResult.Success)
                throw new ValidationException(vr, this, value);
        }
    }

    // Summary:
    //     Represents the exception that occurred during validation of a member that
    //     is marked with a validation attribute.
    public class ValidationException : Exception
    {
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationException
        //     class.
        public ValidationException() : base() { }
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationException
        //     class with the specified error message.
        //
        // Parameters:
        //   message:
        //     The localized message describing the exception.
        public ValidationException(string message) : base(message) { }
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationException
        //     class with the specified error message and an inner exception.
        //
        // Parameters:
        //   message:
        //     The localized message describing the exception.
        //
        //   innerException:
        //     The object representing an exception that caused the current validation exception.
        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationException
        //     class with the specified error message, the attribute that triggered the
        //     exception, and the invalid value.
        //
        // Parameters:
        //   errorMessage:
        //     The localized message describing the exception.
        //
        //   validatingAttribute:
        //     The attribute that caused the validation exception.
        //
        //   value:
        //     The value that caused the validating attribute to trigger the exception.
        public ValidationException(string errorMessage, ValidationAttribute validatingAttribute, object value)
            : base(errorMessage)
        {
            this.validationAttribute = validatingAttribute;
            this.value = value;
        }
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataAnnotations.ValidationException
        //     class with the specified validation result, the attribute that triggered
        //     the exception, and the invalid value.
        //
        // Parameters:
        //   validationResult:
        //     An object containing information about the validation error.
        //
        //   validatingAttribute:
        //     The attribute that caused the validation exception.
        //
        //   value:
        //     The value that caused the validating attribute to trigger the exception.
        public ValidationException(ValidationResult validationResult, ValidationAttribute validatingAttribute, object value)
            : base()
        {
            this.validationAttribute = validatingAttribute;
            this.validationResult = validationResult;
            this.value = value;
        }

        // Summary:
        //     Gets the validation attribute that caused the validation exception.
        //
        // Returns:
        //     The validation attribute that caused the validation exception.
        public ValidationAttribute ValidationAttribute { get { return validationAttribute; } }
        private ValidationAttribute validationAttribute;
        //
        // Summary:
        //     Gets the object containing information about the validation error.
        //
        // Returns:
        //     This object containing information about the validation error.
        public ValidationResult ValidationResult { get { return validationResult; } }
        private ValidationResult validationResult;
        //
        // Summary:
        //     Gets the value that caused the validating attribute to trigger the exception.
        //
        // Returns:
        //     The value that caused the validating attribute to trigger the exception.
        public object Value { get { return value; } }
        private object value;
    }

    // Summary:
    //     Provides members to help validate objects and members using values from the
    //     associated System.ComponentModel.DataAnnotations.ValidationAttribute attribute.
    public static class Validator
    {
        /*
        // Summary:
        //     Determines whether the specified object is valid.
        //
        // Parameters:
        //   instance:
        //     The object to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        //   validationResults:
        //     A collection to store validation results.
        //
        // Returns:
        //     true if the object is valid; otherwise, false.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     instance is null.
        //
        //   System.ArgumentException:
        //     instance does not equal the System.ComponentModel.DataAnnotations.ValidationContext.ObjectInstance
        //     on validationContext.
        public static bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult> validationResults);
        //
        // Summary:
        //     Determines whether the specified object is valid and, if requested, validates
        //     all of the properties of the object.
        //
        // Parameters:
        //   instance:
        //     The object to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        //   validationResults:
        //     A collection to store validation results.
        //
        //   validateAllProperties:
        //     A value that indicates whether all immediate properties of the object are
        //     validated.
        //
        // Returns:
        //     true if the object is valid; otherwise, false.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     instance is null.
        //
        //   System.ArgumentException:
        //     instance does not equal the System.ComponentModel.DataAnnotations.ValidationContext.ObjectInstance
        //     on validationContext.
        public static bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult> validationResults, bool validateAllProperties);
        //
        // Summary:
        //     Determines whether the specified property value is valid.
        //
        // Parameters:
        //   value:
        //     The value to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        //   validationResults:
        //     A collection to store validation results.
        //
        // Returns:
        //     true if the property is valid; otherwise, false.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     System.ComponentModel.DataAnnotations.ValidationContext.MemberName of validationContext
        //     is not a valid property.
        */
        public static bool TryValidateProperty(object value, ValidationContext validationContext, ICollection<ValidationResult> validationResults)
        {
            bool result = false;

            foreach (var propInfo in validationContext.ObjectType.GetProperties())
            {
                if (string.Equals(propInfo.Name, validationContext.MemberName, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var ca in propInfo.GetCustomAttributes(true))
                    {
                        if (ca is RequiredAttribute)
                        {
                            RequiredAttribute ra = ca as RequiredAttribute;
                            if (value == null)
                            {
                                result = false;
                                validationResults.Add(new ValidationResult(ra.ErrorMessage, new string[] { validationContext.MemberName }));
                            }
                        }
                        else if (ca is ValidationAttribute)
                        {
                            ValidationAttribute va = ca as ValidationAttribute;
                            ValidationResult vr = va.GetValidationResult(value, validationContext);
                            if (vr != ValidationResult.Success)
                            {
                                result = false;
                                validationResults.Add(vr);
                            }
                        }
                    }
                }
            }

            return result;
        }
        /*
        //
        // Summary:
        //     Determines whether a specified value is valid against a collection of validation
        //     attributes.
        //
        // Parameters:
        //   value:
        //     The value to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        //   validationResults:
        //     A collection to store validation results.
        //
        //   validationAttributes:
        //     The collection of validation attributes to use to determine if value is valid.
        //
        // Returns:
        //     true if value is valid against the validation attributes; otherwise, false.
        public static bool TryValidateValue(object value, ValidationContext validationContext, ICollection<ValidationResult> validationResults, IEnumerable<ValidationAttribute> validationAttributes);
        //
        // Summary:
        //     Determines whether the specified object is valid and throws a System.ComponentModel.DataAnnotations.ValidationException
        //     if the object is not valid.
        //
        // Parameters:
        //   instance:
        //     The object to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     instance is null.-or-validationContext is null.
        //
        //   System.ArgumentException:
        //     instance does not equal the System.ComponentModel.DataAnnotations.ValidationContext.ObjectInstance
        //     on validationContext.
        //
        //   System.ComponentModel.DataAnnotations.ValidationException:
        //     instance is not valid.
        public static void ValidateObject(object instance, ValidationContext validationContext);
        //
        // Summary:
        //     Determines whether the specified object is valid and, if requested, whether
        //     all of the properties on the object are valid, and throws a System.ComponentModel.DataAnnotations.ValidationException
        //     if the object is not valid.
        //
        // Parameters:
        //   instance:
        //     The object to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        //   validateAllProperties:
        //     A value that indicates whether all immediate properties of the object are
        //     validated.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     instance is null.-or-validationContext is null.
        //
        //   System.ArgumentException:
        //     instance does not equal the System.ComponentModel.DataAnnotations.ValidationContext.ObjectInstance
        //     on validationContext.
        //
        //   System.ComponentModel.DataAnnotations.ValidationException:
        //     instance or at least one of its properties is not valid.
        public static void ValidateObject(object instance, ValidationContext validationContext, bool validateAllProperties);
        //
        // Summary:
        //     Determines whether the specified property value is valid and throws a System.ComponentModel.DataAnnotations.ValidationException
        //     if the property is not valid.
        //
        // Parameters:
        //   value:
        //     The value to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     System.ComponentModel.DataAnnotations.ValidationContext.MemberName of validationContext
        //     is not a valid property.
        //
        //   System.ComponentModel.DataAnnotations.ValidationException:
        //     value is not valid.
        public static void ValidateProperty(object value, ValidationContext validationContext);
        //
        // Summary:
        //     Determines whether a specified value is valid against a collection of validation
        //     attributes and throws a System.ComponentModel.DataAnnotations.ValidationException
        //     if the value is not valid.
        //
        // Parameters:
        //   value:
        //     The value to validate.
        //
        //   validationContext:
        //     An object that contains information about the validation request.
        //
        //   validationAttributes:
        //     The collection of validation attributes to use to determine if value is valid.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     validationContext is null.
        //
        //   System.ComponentModel.DataAnnotations.ValidationException:
        //     value is not valid.
        public static void ValidateValue(object value, ValidationContext validationContext, IEnumerable<ValidationAttribute> validationAttributes);*/
    }
}

namespace System.ComponentModel
{
    // Summary:
    //     Defines properties that data entity classes can implement to provide custom
    //     validation support.
    public interface IDataErrorInfo
    {
        // Summary:
        //     Gets a message that describes any validation errors for the object.
        //
        // Returns:
        //     The validation error on the object, or null or System.String.Empty if there
        //     are no errors present.
        string Error { get; }

        // Summary:
        //     Gets a message that describes any validation errors for the specified property
        //     or column name.
        //
        // Parameters:
        //   columnName:
        //     The name of the property or column to retrieve validation errors for.
        //
        // Returns:
        //     The validation error on the specified property, or null or System.String.Empty
        //     if there are no errors present.
        string this[string columnName] { get; }
    }


    public interface INotifyDataErrorInfo
    {
        // Summary:
        //     Gets a value that indicates whether the object has validation errors.
        //
        // Returns:
        //     true if the object currently has validation errors; otherwise, false.
        bool HasErrors { get; }

        // Summary:
        //     Occurs when the validation errors have changed for a property or for the
        //     entire object.
        event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        // Summary:
        //     Gets the validation errors for a specified property or for the entire object.
        //
        // Parameters:
        //   propertyName:
        //     The name of the property to retrieve validation errors for, or null or System.String.Empty
        //     to retrieve errors for the entire object.
        //
        // Returns:
        //     The validation errors for the property or object.
        IEnumerable GetErrors(string propertyName);
    }

#if !WP7
    public sealed class DataErrorsChangedEventArgs : EventArgs
    {
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DataErrorsChangedEventArgs
        //     class.
        //
        // Parameters:
        //   propertyName:
        //     The name of the property for which the errors changed, or null or System.String.Empty
        //     if the errors affect multiple properties.
        public DataErrorsChangedEventArgs(string propertyName) { this.propertyName = propertyName; }

        // Summary:
        //     Gets the name of the property for which the errors changed, or null or System.String.Empty
        //     if the errors affect multiple properties.
        //
        // Returns:
        //     The name of the affected property, or null or System.String.Empty if the
        //     errors affect multiple properties.
        public string PropertyName { get { return propertyName; } }
        private string propertyName;
    }
#endif
}