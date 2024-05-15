$(function () {
    $('#cbSelectAll').on('click', (function () {
        $('.rowCheckbox').prop('checked', $(this).prop('checked'));
    }));

    $('.rowCheckbox').on('click', function () {
        var allChecked = true;
        $('.rowCheckbox').each(function () {
            if (!$(this).prop('checked')) {
                allChecked = false;
                return false;
            }
        });
        $('#cbSelectAll').prop('checked', allChecked);
    });
});

function handleStatusChange(selectElement) {
    var formId = 'formStatus_' + selectElement.getAttribute('name').split('[')[1].replace(']', '');
    var form = document.getElementById(formId);
    if (form) {
        form.submit();
    }
}

document.addEventListener('DOMContentLoaded', function () {
    var selectedStatus = document.getElementById('selectedStatus');
    var interviewColumns = document.getElementById('interviewColumns');

    function handleStatusChangeInEdit() {
        var selectedValue = selectedStatus.value;

        if (selectedValue === '2' || selectedValue === '3' || selectedValue === '4') {
            interviewColumns.style.display = 'block';
        } else {
            interviewColumns.style.display = 'none';
        }
    }

    selectedStatus.addEventListener('change', handleStatusChangeInEdit);
    selectedStatus.dispatchEvent(new Event('change'));
})

function handleRemoveFromList(applicationId) {
    console.log(applicationId)
    var form = document.getElementById('delete_' + applicationId)

    if (form) {
        form.submit();
    }
}

function validateProfileForm(event) {
    event.preventDefault();

    if (validateUploadFiles()) {
        document.getElementById('profile-form').submit();
    } else {
        console.log('Invalid Upload files');
    }
}

function validateApplicationForm(event) {
    event.preventDefault();

    if (validateUploadFiles()) {
        document.getElementById('application-form').submit();
    } else {
        console.log('Invalid Upload files');
    }
}

function validateUploadFiles() {
    var valResume = document.getElementById('inResume');
    var valCoverLetter = document.getElementById('inCoverLetter');

    if (valResume.files.length === 0 && valCoverLetter.files.length === 0) {
        return true;
    }

    if (valResume.files.length > 1 || valCoverLetter.files.length > 1) {
        alert('Only accept ONE PDF file for Document');
        return false;
    }

    if (valResume.files.length === 1) {
        var resumeFile = valResume.files[0];
        if (!validateFileSize(resumeFile, 2)) {
            alert('Maximum file size is 2MB');
            return false;
        }
        var resumeFileName = resumeFile.name;
        var lowerResumeFile = resumeFileName.toLowerCase();
        if (!lowerResumeFile.endsWith('.pdf')) {
            alert('Only accept PDF file for Resume');
            return false;
        }
    }

    if (valCoverLetter.files.length === 1) {
        var coverLetterFile = valCoverLetter.files[0];
        if (!validateFileSize(coverLetterFile, 2)) {
            alert('Maximum file size is 2MB');
            return false;
        }
        var coverLetterFileName = coverLetterFile.name;
        var lowerCoverLetterFile = coverLetterFileName.toLowerCase();
        if (!lowerCoverLetterFile.endsWith('.pdf')) {
            alert('Only accept PDF file for Cover Letter');
            return false;
        }
    }

    return true;
}

function validateFileSize(file, maxSize) {
    var maxSizeBytes = maxSize * 1024 * 1024;
    return file.size <= maxSizeBytes;
}

