#include <iostream>

#include "cfgmgr32_common.h"

void print_details(DEVINST devInst) {
    CONFIGRET cr;

    std::wstring buffer{};
    buffer.resize(256);

    cr = CM_Get_Device_ID(devInst, &buffer[0], buffer.size(), 0);
    if (cr != CR_SUCCESS) {
        std::wcerr << L"CM_Get_Device_ID(" << devInst << L", buffer, 256, 0) returned " << cr << std::endl;
        return;
    }
    buffer.resize(std::wcslen(buffer.c_str()));

    ULONG status = 0;
    ULONG problemCode = 0;
    cr = CM_Get_DevNode_Status(&status, &problemCode, devInst, 0);
    if (cr != CR_SUCCESS) {
        std::wcerr << L"CM_Get_DevNode_Status(&status, &probCode, " << devInst << L", 0) returned " << cr << std::endl;
        status = -1;
        problemCode = -1;
    }

    std::wcout
        << L"Device " << devInst
        << L": " << buffer
        << L" (status=" << std::hex << status << std::dec
        << L"; problem=" << problemCode
        << L")" << std::endl;
}
