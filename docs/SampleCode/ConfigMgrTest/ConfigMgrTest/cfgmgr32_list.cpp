#include <Windows.h>
#include <cfgmgr32.h>
#include <iostream>
#include <vector>

#include "cfgmgr32_list.h"
#include "cfgmgr32_common.h"

void cfgmgr32_list() {
    std::vector<std::wstring> deviceIDs;

    ULONG bufferSize = 0;
    CONFIGRET cr = CM_Get_Device_ID_List_Size(&bufferSize, nullptr, CM_GETIDLIST_FILTER_NONE);
    if (cr != CR_SUCCESS) {
        std::wcerr << L"CM_Get_Device_ID_List_Size(&bufferSize, nullptr, CM_GETIDLIST_FILTER_NONE) return " << cr << std::endl;
        return;
    }

    std::vector<wchar_t> buffer(bufferSize);
    cr = CM_Get_Device_ID_List(nullptr, buffer.data(), bufferSize, CM_GETIDLIST_FILTER_NONE);
    if (cr != CR_SUCCESS) {
        std::wcerr
            << L"CM_Get_Device_ID_List(nullptr, buffer, " << bufferSize
            << L", CM_GETIDLIST_FILTER_NONE) returned " << cr
            << std::endl;
        return;
    }

    wchar_t* current = buffer.data();
    while (*current != L'\0') {
        DEVINST devInst;
        cr = CM_Locate_DevNode(&devInst, (DEVINSTID)current, CM_LOCATE_DEVNODE_PHANTOM);
        if (cr == CR_SUCCESS) {
            std::wcout << current << std::endl;
        } else {
            std::wcout << current << L" (error " << cr << L")" << std::endl;
        }
        print_details(devInst);

        current += std::wcslen(current) + 1;

    }
}
