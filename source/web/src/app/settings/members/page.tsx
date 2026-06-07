'use client'

import { useEffect, useState } from 'react'
import { apiClient } from '@/api/client'
import styles from './members.module.css'

interface Member {
  userId: string
  username: string
  role: 'Owner' | 'AdultMember' | 'RestrictedMember'
  mfaEnabled: boolean
  isActive: boolean
}

interface MfaToggleDialogState {
  member: Member
  targetEnabled: boolean
}

export default function MembersSettingsPage() {
  const [members, setMembers] = useState<Member[]>([])
  const [loading, setLoading] = useState(false)
  const [dialog, setDialog] = useState<MfaToggleDialogState | null>(null)
  const [toggleError, setToggleError] = useState<string | null>(null)
  const [toggleLoading, setToggleLoading] = useState(false)

  async function loadMembers() {
    setLoading(true)
    try {
      const res = await apiClient.get<Member[]>('/admin/users')
      setMembers(res)
    } catch {
      // Members will show empty; the parent page should handle auth errors.
    } finally {
      setLoading(false)
    }
  }

  // Load members on first render.
  useEffect(() => {
    void loadMembers()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function confirmMfaToggle() {
    if (!dialog) return

    setToggleError(null)
    setToggleLoading(true)

    try {
      await apiClient.patch(`/admin/users/${dialog.member.userId}/mfa`, {
        enabled: dialog.targetEnabled,
      })

      setMembers(prev =>
        prev.map(m =>
          m.userId === dialog.member.userId
            ? { ...m, mfaEnabled: dialog.targetEnabled }
            : m
        )
      )
      setDialog(null)
    } catch (err) {
      setToggleError(err instanceof Error ? err.message : 'Failed to update MFA setting.')
    } finally {
      setToggleLoading(false)
    }
  }

  const roleLabel: Record<Member['role'], string> = {
    Owner: 'Owner',
    AdultMember: 'Adult Member',
    RestrictedMember: 'Restricted Member',
  }

  return (
    <main className={styles.container}>
      <h1 className={styles.pageTitle}>Household Members</h1>

      {loading ? (
        <p className={styles.loadingText}>Loading members…</p>
      ) : (
        <div className={styles.tableWrapper}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th className={styles.th}>Username</th>
                <th className={styles.th}>Role</th>
                <th className={styles.th}>MFA</th>
                <th className={styles.th}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {members.map(member => (
                <tr key={member.userId} className={styles.tr}>
                  <td className={styles.td}>{member.username}</td>
                  <td className={styles.td}>{roleLabel[member.role]}</td>
                  <td className={styles.td}>
                    <span className={member.mfaEnabled ? styles.badgeOn : styles.badgeOff}>
                      {member.mfaEnabled ? 'Enabled' : 'Disabled'}
                    </span>
                  </td>
                  <td className={styles.td}>
                    <button
                      className={member.mfaEnabled ? styles.disableButton : styles.enableButton}
                      onClick={() =>
                        setDialog({ member, targetEnabled: !member.mfaEnabled })
                      }
                    >
                      {member.mfaEnabled ? 'Disable MFA' : 'Enable MFA'}
                    </button>
                  </td>
                </tr>
              ))}
              {members.length === 0 && (
                <tr>
                  <td colSpan={4} className={styles.emptyCell}>No members found.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {dialog && (
        <div className={styles.dialogOverlay} role="dialog" aria-modal="true">
          <div className={styles.dialog}>
            <h2 className={styles.dialogTitle}>
              {dialog.targetEnabled ? 'Enable MFA' : 'Disable MFA'}
            </h2>
            <p className={styles.dialogBody}>
              {dialog.targetEnabled
                ? `Enable MFA for ${dialog.member.username}? They will be prompted to set up an authenticator app on their next login.`
                : `Disable MFA for ${dialog.member.username}? Their authenticator app will no longer be required to log in.`}
            </p>

            {toggleError && (
              <p role="alert" className={styles.error}>{toggleError}</p>
            )}

            <div className={styles.dialogActions}>
              <button
                onClick={() => setDialog(null)}
                disabled={toggleLoading}
                className={styles.cancelButton}
              >
                Cancel
              </button>
              <button
                onClick={confirmMfaToggle}
                disabled={toggleLoading}
                className={dialog.targetEnabled ? styles.enableButton : styles.disableButton}
              >
                {toggleLoading ? 'Saving…' : 'Confirm'}
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  )
}
