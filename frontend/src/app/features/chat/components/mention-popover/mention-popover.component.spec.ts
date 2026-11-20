/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file mention-popover.component.spec.ts — Unit tests cho MentionPopoverComponent (Sprint 1 - PBI-CHAT-03)
 *
 * ## Mục Tiêu Kiểm Thử
 * Đảm bảo MentionPopoverComponent lọc chính xác danh sách thành viên theo từ khóa sau ký tự '@',
 * phản hồi các phím mũi tên Lên/Xuống, Enter, Tab, Escape và phát sự kiện chọn thành viên.
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { MentionPopoverComponent } from './mention-popover.component';
import { Member, MemberType } from '../../../organization/models/organization.models';

describe('MentionPopoverComponent', () => {
  let component: MentionPopoverComponent;
  let fixture: ComponentFixture<MentionPopoverComponent>;

  const mockMembers: Member[] = [
    {
      id: 'mem-1',
      channelId: 'chan-1',
      type: MemberType.Human,
      displayName: 'Alice Johnson',
      createdDate: '2026-11-19T00:00:00Z',
    },
    {
      id: 'mem-2',
      channelId: 'chan-1',
      type: MemberType.Agent,
      displayName: 'Support Agent AI',
      createdDate: '2026-11-19T00:00:00Z',
    },
    {
      id: 'mem-3',
      channelId: 'chan-1',
      type: MemberType.Agent,
      displayName: 'Coder Agent',
      createdDate: '2026-11-19T00:00:00Z',
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MentionPopoverComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MentionPopoverComponent);
    component = fixture.componentInstance;
  });

  it('khởi tạo thành công component', () => {
    expect(component).toBeTruthy();
  });

  it('lọc danh sách thành viên theo filterText chính xác không phân biệt hoa thường', () => {
    fixture.componentRef.setInput('members', mockMembers);
    fixture.componentRef.setInput('filterText', 'agent');
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();

    expect(component.filteredMembers().length).toBe(2);
    expect(component.filteredMembers()[0].displayName).toBe('Support Agent AI');
    expect(component.filteredMembers()[1].displayName).toBe('Coder Agent');
  });

  it('điều hướng ArrowDown và ArrowUp thay đổi selectedIndex', () => {
    fixture.componentRef.setInput('members', mockMembers);
    fixture.componentRef.setInput('filterText', '');
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();

    expect(component.selectedIndex()).toBe(0);

    // ArrowDown: 0 -> 1
    const eventDown = new KeyboardEvent('keydown', { key: 'ArrowDown' });
    const handledDown = component.handleKeyDown(eventDown);
    expect(handledDown).toBe(true);
    expect(component.selectedIndex()).toBe(1);

    // ArrowUp: 1 -> 0
    const eventUp = new KeyboardEvent('keydown', { key: 'ArrowUp' });
    const handledUp = component.handleKeyDown(eventUp);
    expect(handledUp).toBe(true);
    expect(component.selectedIndex()).toBe(0);

    // ArrowUp wrap around: 0 -> 2
    component.handleKeyDown(eventUp);
    expect(component.selectedIndex()).toBe(2);
  });

  it('phát ra sự kiện selectMember khi nhấn Enter', () => {
    fixture.componentRef.setInput('members', mockMembers);
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();

    let selected: Member | null = null;
    component.selectMember.subscribe((m) => {
      selected = m;
    });

    const eventEnter = new KeyboardEvent('keydown', { key: 'Enter' });
    const handled = component.handleKeyDown(eventEnter);

    expect(handled).toBe(true);
    expect(selected).toEqual(mockMembers[0]);
  });

  it('phát ra sự kiện closePopover khi nhấn Escape', () => {
    fixture.componentRef.setInput('members', mockMembers);
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();

    let closed = false;
    component.closePopover.subscribe(() => {
      closed = true;
    });

    const eventEsc = new KeyboardEvent('keydown', { key: 'Escape' });
    const handled = component.handleKeyDown(eventEsc);

    expect(handled).toBe(true);
    expect(closed).toBe(true);
  });

  it('phát sự kiện selectMember khi click vào item', () => {
    fixture.componentRef.setInput('members', mockMembers);
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();

    let selected: Member | null = null;
    component.selectMember.subscribe((m) => {
      selected = m;
    });

    const compiled = fixture.nativeElement as HTMLElement;
    const items = compiled.querySelectorAll('.member-item');
    expect(items.length).toBe(3);

    (items[1] as HTMLElement).click();
    expect(selected).toEqual(mockMembers[1]);
  });
});
