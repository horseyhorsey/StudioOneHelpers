// PDF Interop functionality for Studio One Helpers
// This file handles PDF generation for both Commands and Plugins

// Ensure pdfInterop is available when the page loads
window.addEventListener('DOMContentLoaded', function() {
    window.pdfInterop = {
        printCmds: function (commandsData) {
            try {
                if (!window.jspdf) {
                    console.error('jsPDF library not loaded');
                    return;
                }
                
                const { jsPDF } = window.jspdf;
                const doc = new jsPDF();

                // Build rows for the table
                const rows = commandsData.map(item => [
                    item.sectionName || '',
                    item.commandName || '',
                    item.shortcut || ''
                ]);

                doc.setFontSize(8);

                // Create table
                doc.autoTable({
                    head: [['Section', 'Command', 'Shortcut']],
                    body: rows,
                    startY: 2,
                    styles: { fontSize: 8 },
                    margin: { top: 10, right: 10, bottom: 10, left: 10 }
                });

                doc.save('S1_Shortcuts.pdf');
            } catch (error) {
                console.error('Error generating PDF:', error);
                throw error;
            }
        },
        
        printPlugins: function (pluginsData) {
            try {
                if (!window.jspdf) {
                    console.error('jsPDF library not loaded');
                    return;
                }
                
                const { jsPDF } = window.jspdf;
                const doc = new jsPDF();

                // Build rows for the table
                const rows = pluginsData.map(item => [
                    item.category || '',
                    item.name || '',
                    item.vendor || '',
                    item.version || '',
                    item.folder || ''
                ]);

                doc.setFontSize(8);

                // Create table
                doc.autoTable({
                    head: [['Category', 'Name', 'Vendor', 'Version', 'Folder']],
                    body: rows,
                    startY: 2,
                    styles: { fontSize: 8 },
                    margin: { top: 10, right: 10, bottom: 10, left: 10 }
                });

                doc.save('S1_Plugins.pdf');
            } catch (error) {
                console.error('Error generating PDF:', error);
                throw error;
            }
        },
        
        printPresets: function (presetsData, categoryName) {
            try {
                if (!window.jspdf) {
                    console.error('jsPDF library not loaded');
                    return;
                }
                
                const { jsPDF } = window.jspdf;
                const doc = new jsPDF();

                // Build rows for the table
                const rows = presetsData.map(item => [
                    item.vendor || '',
                    item.classId || '',
                    item.title || '',
                    item.creator || '',
                    item.subFolder || ''
                ]);

                doc.setFontSize(8);

                // Create table
                doc.autoTable({
                    head: [['Vendor', 'Class ID', 'Title', 'Creator', 'SubFolder']],
                    body: rows,
                    startY: 2,
                    styles: { fontSize: 8 },
                    margin: { top: 10, right: 10, bottom: 10, left: 10 }
                });

                const fileName = `S1_${categoryName}_Presets.pdf`;
                doc.save(fileName);
            } catch (error) {
                console.error('Error generating PDF:', error);
                throw error;
            }
        },
        
        downloadFile: function (fileName, base64Content) {
            try {
                // Convert base64 to blob
                const byteCharacters = atob(base64Content);
                const byteNumbers = new Array(byteCharacters.length);
                for (let i = 0; i < byteCharacters.length; i++) {
                    byteNumbers[i] = byteCharacters.charCodeAt(i);
                }
                const byteArray = new Uint8Array(byteNumbers);
                const blob = new Blob([byteArray], { type: 'application/octet-stream' });
                
                // Create download link
                const url = window.URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.download = fileName;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                window.URL.revokeObjectURL(url);
            } catch (error) {
                console.error('Error downloading file:', error);
                throw error;
            }
        }
    };
    
    // Make downloadFile available globally
    window.downloadFile = window.pdfInterop.downloadFile;
    
    // Add file input click function
    window.clickFileInput = function(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.click();
        }
    };
    
    // Add clear all localStorage data function
    window.clearAllLocalStorageData = function() {
        try {
            // Clear all Studio One Helpers related localStorage items
            const keysToRemove = [
                'CommandsData',
                'CommandsData_ImportTime',
                'PluginsData',
                'PluginsData_ImportTime',
                'PresetData',
                'PresetData_ImportTime',
                'PresetData_Compressed'
            ];
            
            // Also clear category-specific preset data
            const categoryKeys = [
                'PresetData_Artist',
                'PresetData_AudioEffect', 
                'PresetData_AudioSynth',
                'PresetData_FXChain',
                'PresetData_MusicEffect',
                'PresetData_PatternBank',
                'PresetData_TrackPreset'
            ];
            
            keysToRemove.push(...categoryKeys);
            
            keysToRemove.forEach(key => {
                localStorage.removeItem(key);
            });
            
            console.log('All Studio One Helpers data cleared from localStorage');
        } catch (error) {
            console.error('Error clearing localStorage:', error);
            throw error;
        }
    };
    
    // Add scroll functions for Guide page
    window.scrollIntoView = function(elementId) {
        try {
            const element = document.getElementById(elementId);
            if (element) {
                element.scrollIntoView({ 
                    behavior: 'smooth', 
                    block: 'start' 
                });
            }
        } catch (error) {
            console.error('Error scrolling to element:', error);
        }
    };
    
    window.scrollToTop = function() {
        try {
            window.scrollTo({ 
                top: 0, 
                behavior: 'smooth' 
            });
        } catch (error) {
            console.error('Error scrolling to top:', error);
        }
    };
});
