// PDF Interop functionality for Studio One Helpers
// This file handles PDF generation for both Commands and Plugins

// Ensure pdfInterop is available when the page loads
window.addEventListener('DOMContentLoaded', function() {
    // Wait for jsPDF to be available
    if (typeof window.jspdf === 'undefined') {
        console.log('jsPDF not yet loaded, retrying in 100ms...');
        setTimeout(() => {
            initializePdfInterop();
        }, 100);
        return;
    }
    
    initializePdfInterop();
});

function initializePdfInterop() {
    window.pdfInterop = {
        printCmds: function (commandsData) {
            try {                
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
        
        printStickers: function (layoutData, gridRows, gridColumns, buttonWidth, buttonHeight, sizeUnit) {
            try {
                if (!window.jspdf) {
                    console.error('jsPDF library not loaded');
                    return;
                }
                
                const { jsPDF } = window.jspdf;
                
                // Debug logging
                console.log('Button dimensions:', { buttonWidth, buttonHeight, sizeUnit });
                
                // Check if we need landscape orientation
                const margin = 10; // 10mm margin for printer
                const testGridWidth = buttonWidth * gridColumns;
                const testGridHeight = buttonHeight * gridRows;
                
                // Warn if too many columns
                if (gridColumns > 13) {
                    console.warn(`Too many columns (${gridColumns}). Maximum recommended is 13 columns.`);
                    console.warn('Consider reducing button size or number of columns for better layout.');
                }
                
                // A4 dimensions: 210mm x 297mm (portrait), 297mm x 210mm (landscape)
                const portraitWidth = 210 - (margin * 2);
                const landscapeWidth = 297 - (margin * 2);
                
                const needsLandscape = testGridWidth > portraitWidth && testGridWidth <= landscapeWidth;
                
                const doc = needsLandscape ? new jsPDF('landscape') : new jsPDF();
                
                if (needsLandscape) {
                    console.log('Switching to landscape orientation for better fit');
                }
                
                // Convert dimensions to PDF points
                // 1 mm = 2.83465 points, 1 cm = 10 mm
                let widthInMm = buttonWidth;
                let heightInMm = buttonHeight;
                
                if (sizeUnit === 'cm') {
                    widthInMm = buttonWidth * 10; // Convert cm to mm
                    heightInMm = buttonHeight * 10;
                }
                // If sizeUnit is 'mm', values are already in mm, no conversion needed
                
                // Convert mm to points: 1 mm = 2.83465 points
                // But jsPDF might be expecting different units, let's try direct mm values
                const buttonWidthPoints = widthInMm;
                const buttonHeightPoints = heightInMm;
                
                // Debug logging
                console.log('Converted dimensions:', { widthInMm, heightInMm, buttonWidthPoints, buttonHeightPoints });            
                
                // Calculate grid dimensions
                const totalGridWidth = buttonWidthPoints * gridColumns;
                const totalGridHeight = buttonHeightPoints * gridRows;
                
                // Get page dimensions (A4 = 210mm x 297mm)
                const pageWidth = doc.internal.pageSize.getWidth();
                const pageHeight = doc.internal.pageSize.getHeight();
                
                // Check if grid fits on page width
                const availableWidth = pageWidth - (margin * 2);
                
                if (totalGridWidth > availableWidth) {
                    console.warn(`Grid width (${totalGridWidth.toFixed(1)}mm) exceeds available width (${availableWidth.toFixed(1)}mm). Consider reducing button size or columns.`);
                }
                
                // Position grid from top with margin
                const startX = margin;
                const startY = margin;
                
                // Draw each button
                layoutData.forEach(button => {
                    const x = startX + (button.column * buttonWidthPoints);
                    const y = startY + (button.row * buttonHeightPoints);
                    
                    // Skip buttons that would go off the page
                    if (x + buttonWidthPoints > pageWidth - margin || y + buttonHeightPoints > pageHeight - margin) {
                        console.warn(`Button at row ${button.row}, col ${button.column} would go off page. Skipping.`);
                        return;
                    }
                    
                    // Set button color
                    doc.setFillColor(button.color);
                    doc.setDrawColor(0, 0, 0);
                    doc.setLineWidth(0);
                    
                    // Draw button shape
                    if (button.shape === 'circle') {
                        const centerX = x + (buttonWidthPoints / 2);
                        const centerY = y + (buttonHeightPoints / 2);
                        const radius = Math.min(buttonWidthPoints, buttonHeightPoints) / 2 - 1;
                        doc.circle(centerX, centerY, radius, 'FD');
                    } else {
                        doc.rect(x, y, buttonWidthPoints, buttonHeightPoints, 'FD');
                    }
                    
                    // Add text if assigned
                    const displayText = button.customName || button.assignedText;
                    if (displayText && displayText.trim() !== '') {
                        doc.setTextColor(255, 255, 255); // White text
                        
                        // Calculate optimal font size based on button dimensions
                        const baseFontSize = Math.max(6, Math.min(16, Math.min(buttonWidthPoints, buttonHeightPoints) / 6));
                        doc.setFontSize(baseFontSize);
                        
                        // Calculate text area with padding
                        const padding = 2; // 2mm padding
                        const textAreaWidth = buttonWidthPoints - (padding * 2);
                        const textAreaHeight = buttonHeightPoints - (padding * 2);
                        
                        // Use splitTextToSize for automatic text wrapping
                        const lines = doc.splitTextToSize(displayText, textAreaWidth);
                        
                        // Calculate line height and total text height
                        const lineHeight = baseFontSize * 1.2;
                        const totalTextHeight = lines.length * lineHeight;
                        
                        // If text is too tall, reduce font size
                        let fontSize = baseFontSize;
                        if (totalTextHeight > textAreaHeight) {
                            fontSize = Math.max(4, (textAreaHeight / lines.length) / 1.2);
                            doc.setFontSize(fontSize);
                            const newLines = doc.splitTextToSize(displayText, textAreaWidth);
                            lines.length = 0;
                            lines.push(...newLines);
                        }
                        
                        // Center text vertically and horizontally
                        const textX = x + (buttonWidthPoints / 2);
                        const startY = y + (buttonHeightPoints / 2) - (lines.length * fontSize * 1.2 / 2) + (fontSize / 3);
                        
                        // Draw each line
                        lines.forEach((line, index) => {
                            const lineY = startY + (index * fontSize * 1.2);
                            doc.text(line, textX, lineY, { align: 'center' });
                        });
                    }
                });
                
                doc.save('Controller_Stickers.pdf');
            } catch (error) {
                console.error('Error generating sticker PDF:', error);
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
}
